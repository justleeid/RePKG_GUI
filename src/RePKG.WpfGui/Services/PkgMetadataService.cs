using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RePKG.Application.Package;
using RePKG.WpfGui.Models;

namespace RePKG.WpfGui.Services;

/// <summary>
/// PKG 元数据提取服务实现
/// </summary>
public class PkgMetadataService : IPkgMetadataService
{
    /// <inheritdoc/>
    public async Task<WallpaperItem?> ExtractMetadataAsync(string pkgPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ExtractMetadata(pkgPath, cancellationToken), cancellationToken);
    }

    private static WallpaperItem? ExtractMetadata(string pkgPath, CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = new FileInfo(pkgPath);
            if (!fileInfo.Exists)
                return null;

            cancellationToken.ThrowIfCancellationRequested();

            // 使用 RePKG 读取 PKG
            var reader = new PackageReader { ReadEntryBytes = false };

            Core.Package.Package package;
            using (var stream = File.OpenRead(pkgPath))
            using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                package = reader.ReadFrom(binaryReader);

                cancellationToken.ThrowIfCancellationRequested();

                // 手动读取需要的条目数据
                foreach (var entry in package.Entries)
                {
                    if (entry.FullPath.EndsWith("project.json", StringComparison.OrdinalIgnoreCase) ||
                        entry.FullPath.EndsWith("preview.jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.Seek(entry.Offset + package.HeaderSize, SeekOrigin.Begin);
                        entry.Bytes = binaryReader.ReadBytes(entry.Length);
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 提取 project.json
            var projectEntry = package.Entries.FirstOrDefault(
                e => e.FullPath.EndsWith("project.json", StringComparison.OrdinalIgnoreCase));

            ProjectInfo? projectInfo = null;
            if (projectEntry?.Bytes != null)
            {
                var json = Encoding.UTF8.GetString(projectEntry.Bytes);
                projectInfo = JsonSerializer.Deserialize<ProjectInfo>(json);
            }

            // 提取 preview.jpg
            var previewEntry = package.Entries.FirstOrDefault(
                e => e.FullPath.EndsWith("preview.jpg", StringComparison.OrdinalIgnoreCase));

            return new WallpaperItem
            {
                Title = projectInfo?.Title ?? Path.GetFileNameWithoutExtension(pkgPath),
                Author = projectInfo?.Author ?? "Unknown",
                Type = projectInfo?.Type ?? "unknown",
                PkgPath = pkgPath,
                FileSize = fileInfo.Length,
                WorkshopId = projectInfo?.WorkshopId ?? string.Empty,
                AuthorSteamId = projectInfo?.AuthorSteamId,
                Tags = projectInfo?.Tags ?? Array.Empty<string>(),
                PreviewBytes = previewEntry?.Bytes,
                ScanTime = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 损坏或无法解析的 PKG 返回失败状态
            return new WallpaperItem
            {
                Title = Path.GetFileNameWithoutExtension(pkgPath),
                PkgPath = pkgPath,
                FileSize = new FileInfo(pkgPath).Length,
                IsScanFailed = true,
                ErrorMessage = ex.Message,
                ScanTime = DateTime.UtcNow
            };
        }
    }
}
