using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RePKG.Application.Package;
using RePKG.Application.Texture;
using RePKG.Core.Package;
using RePKG.Core.Package.Enums;
using RePKG.Core.Texture;
using RePKG.WpfGui.Models;

namespace RePKG.WpfGui.Services;

/// <summary>
/// 解包服务实现
/// </summary>
public class ExtractService : IExtractService
{
    private static readonly TexReader TexReader = TexReader.Default;
    private static readonly TexToImageConverter TexConverter = new();

    /// <inheritdoc/>
    public async Task<ExtractResult> ExtractAsync(
        IEnumerable<WallpaperItem> items,
        ExtractOptions options,
        IProgress<ExtractProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var itemList = items.ToList();
            var successCount = 0;
            var failedCount = 0;
            var skippedCount = 0;
            var errors = new List<ExtractError>();

            for (int i = 0; i < itemList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = itemList[i];
                var progressInfo = new ExtractProgress
                {
                    TotalItems = itemList.Count,
                    CompletedItems = i,
                    FailedItems = failedCount,
                    CurrentItemTitle = item.Title,
                    Status = ExtractItemStatus.Extracting
                };
                progress?.Report(progressInfo);

                try
                {
                    // 创建文件级进度回调
                    var fileProgress = new Progress<(int total, int current, string fileName)>(info =>
                    {
                        progress?.Report(progressInfo with
                        {
                            Status = ExtractItemStatus.Extracting,
                            TotalFiles = info.total,
                            ProcessedFiles = info.current,
                            CurrentFileName = info.fileName
                        });
                    });

                    var result = ExtractSinglePkg(item, options, fileProgress, cancellationToken);
                    switch (result)
                    {
                        case ExtractItemStatus.Completed:
                            successCount++;
                            break;
                        case ExtractItemStatus.Skipped:
                            skippedCount++;
                            break;
                        case ExtractItemStatus.Failed:
                            failedCount++;
                            errors.Add(new ExtractError
                            {
                                PkgPath = item.PkgPath,
                                Title = item.Title,
                                Message = "解包失败"
                            });
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    errors.Add(new ExtractError
                    {
                        PkgPath = item.PkgPath,
                        Title = item.Title,
                        Message = ex.Message
                    });
                }

                progress?.Report(progressInfo with
                {
                    CompletedItems = i + 1,
                    FailedItems = failedCount,
                    Status = ExtractItemStatus.Completed
                });
            }

            stopwatch.Stop();

            return new ExtractResult
            {
                SuccessCount = successCount,
                FailedCount = failedCount,
                SkippedCount = skippedCount,
                Errors = errors,
                Elapsed = stopwatch.Elapsed
            };
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExtractSingleAsync(
        WallpaperItem item,
        ExtractOptions options,
        IProgress<ExtractProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            progress?.Report(new ExtractProgress
            {
                TotalItems = 1,
                CompletedItems = 0,
                CurrentItemTitle = item.Title,
                Status = ExtractItemStatus.Extracting
            });

            // 创建文件级进度回调
            var fileProgress = new Progress<(int total, int current, string fileName)>(info =>
            {
                progress?.Report(new ExtractProgress
                {
                    TotalItems = 1,
                    CompletedItems = 0,
                    CurrentItemTitle = item.Title,
                    Status = ExtractItemStatus.Extracting,
                    TotalFiles = info.total,
                    ProcessedFiles = info.current,
                    CurrentFileName = info.fileName
                });
            });

            var result = ExtractSinglePkg(item, options, fileProgress, cancellationToken);

            progress?.Report(new ExtractProgress
            {
                TotalItems = 1,
                CompletedItems = 1,
                CurrentItemTitle = item.Title,
                Status = result
            });

            return result == ExtractItemStatus.Completed;
        }, cancellationToken);
    }

    private static ExtractItemStatus ExtractSinglePkg(
        WallpaperItem item,
        ExtractOptions options,
        IProgress<(int total, int current, string fileName)>? fileProgress,
        CancellationToken cancellationToken)
    {
        var pkgPath = item.PkgPath;
        if (!File.Exists(pkgPath))
            return ExtractItemStatus.Failed;

        // 计算输出目录
        var outputDirectory = CalculateOutputDirectory(item, options);
        Directory.CreateDirectory(outputDirectory);

        // 读取 PKG（必须设置 ReadEntryBytes = true 以获取条目数据）
        Package package;
        using (var stream = File.OpenRead(pkgPath))
        using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
        {
            var pkgReader = new PackageReader { ReadEntryBytes = true };
            package = pkgReader.ReadFrom(reader);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 过滤条目
        var entries = FilterEntries(package.Entries, options).ToList();

        // 提取每个条目，报告文件级进度
        for (int j = 0; j < entries.Count; j++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = entries[j];
            fileProgress?.Report((entries.Count, j, Path.GetFileName(entry.FullPath)));
            ExtractEntry(entry, outputDirectory, options);
        }

        // 报告完成
        fileProgress?.Report((entries.Count, entries.Count, "完成"));

        return ExtractItemStatus.Completed;
    }

    private static string CalculateOutputDirectory(WallpaperItem item, ExtractOptions options)
    {
        // 单目录模式：所有文件放在同一个目录下
        if (options.SingleDirectory)
            return options.OutputDirectory;

        // 不使用壁纸名称作为子文件夹
        if (!options.UseNameAsFolder)
            return options.OutputDirectory;

        // 使用壁纸标题作为文件夹名，清理非法字符
        var safeName = GetSafeFilename(item.Title);
        return Path.Combine(options.OutputDirectory, safeName);
    }

    private static IEnumerable<PackageEntry> FilterEntries(
        IEnumerable<PackageEntry> entries,
        ExtractOptions options)
    {
        var query = entries.AsEnumerable();

        // 应用扩展名过滤
        if (options.IncludeExtensions?.Length > 0)
        {
            var extensions = options.IncludeExtensions
                .Select(e => e.StartsWith('.') ? e : '.' + e)
                .ToArray();
            query = query.Where(e =>
                extensions.Any(ext => e.FullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
        }

        if (options.ExcludeExtensions?.Length > 0)
        {
            var extensions = options.ExcludeExtensions
                .Select(e => e.StartsWith('.') ? e : '.' + e)
                .ToArray();
            query = query.Where(e =>
                !extensions.Any(ext => e.FullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
        }

        return query;
    }

    private static void ExtractEntry(PackageEntry entry, string outputDirectory, ExtractOptions options)
    {
        // 构建输出路径
        var filePath = Path.Combine(outputDirectory, entry.FullPath);
        var dirPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(dirPath))
            Directory.CreateDirectory(dirPath);

        // 如果是 TEX 文件且启用转换，只输出转换后的图片，不输出原始 TEX
        if (options.ConvertTexToImage && entry.Type == EntryType.Tex)
        {
            ConvertTexToImage(entry.Bytes, filePath, options.OverwriteExisting);
            return; // 不写入原始 .tex 文件
        }

        // 检查是否已存在
        if (!options.OverwriteExisting && File.Exists(filePath))
            return;

        // 流式写入文件（避免大文件内存峰值）
        WriteBytesStreamed(filePath, entry.Bytes);
    }

    /// <summary>
    /// 流式写入字节数组到文件（避免大文件内存峰值）
    /// </summary>
    private static void WriteBytesStreamed(string filePath, byte[] bytes)
    {
        const int bufferSize = 81920; // 80KB 缓冲区
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);
        using var memoryStream = new MemoryStream(bytes);
        memoryStream.CopyTo(fileStream, bufferSize);
    }

    private static void ConvertTexToImage(byte[] texBytes, string originalPath, bool overwrite)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(texBytes), Encoding.UTF8);
            var tex = TexReader.ReadFrom(reader);

            if (tex == null)
                return;

            var format = TexConverter.GetConvertedFormat(tex);
            var outputPath = $"{Path.ChangeExtension(originalPath, null)}.{format.GetFileExtension()}";

            if (!overwrite && File.Exists(outputPath))
                return;

            var result = TexConverter.ConvertToImage(tex);
            File.WriteAllBytes(outputPath, result.Bytes);
        }
        catch (Exception ex)
        {
            // TEX 转换失败不影响主流程，但记录日志
            System.Diagnostics.Debug.WriteLine($"TEX 转换失败: {originalPath}, 错误: {ex.Message}");
        }
    }

    private static string GetSafeFilename(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(safeName) ? "Untitled" : safeName;
    }
}
