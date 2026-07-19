using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RePKG.WpfGui.Models;

namespace RePKG.WpfGui.Services;

/// <summary>
/// PKG 元数据提取服务实现
/// </summary>
public class PkgMetadataService : IPkgMetadataService
{
    private readonly ILogger<PkgMetadataService> _logger;

    public PkgMetadataService(ILogger<PkgMetadataService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<WallpaperItem?> ExtractMetadataAsync(string pkgPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ExtractMetadata(pkgPath, cancellationToken), cancellationToken);
    }

    private WallpaperItem? ExtractMetadata(string pkgPath, CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = new FileInfo(pkgPath);
            if (!fileInfo.Exists)
            {
                _logger.LogWarning("PKG 文件不存在: {Path}", pkgPath);
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // project.json 和 preview.jpg 是与 PKG 同目录的外部文件（Wallpaper Engine 结构）
            var pkgDir = fileInfo.DirectoryName!;
            var projectJsonPath = Path.Combine(pkgDir, "project.json");

            ProjectInfo? projectInfo = null;
            byte[]? previewBytes = null;

            // 读取 project.json（外部文件）
            if (File.Exists(projectJsonPath))
            {
                try
                {
                    var json = File.ReadAllText(projectJsonPath, Encoding.UTF8);
                    projectInfo = JsonSerializer.Deserialize<ProjectInfo>(json);
                    _logger.LogDebug("读取 project.json: {Path}", projectJsonPath);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "project.json 解析失败: {Path}", projectJsonPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "project.json 读取失败: {Path}", projectJsonPath);
                }
            }
            else
            {
                _logger.LogDebug("未找到 project.json: {Path}", projectJsonPath);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 读取 preview.jpg（路径来自 project.json 的 preview 字段）
            if (!string.IsNullOrEmpty(projectInfo?.Preview))
            {
                var previewPath = Path.Combine(pkgDir, projectInfo.Preview);
                if (File.Exists(previewPath))
                {
                    try
                    {
                        previewBytes = File.ReadAllBytes(previewPath);
                        _logger.LogDebug("读取预览图: {Path}", previewPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "预览图读取失败: {Path}", previewPath);
                    }
                }
                else
                {
                    _logger.LogDebug("未找到预览图: {Path}", previewPath);
                }
            }

            // 从目录名提取 Workshop ID（目录名通常就是 Workshop ID）
            var workshopId = projectInfo?.WorkshopId ?? Path.GetFileName(pkgDir);

            return new WallpaperItem
            {
                Title = projectInfo?.Title ?? Path.GetFileNameWithoutExtension(pkgPath),
                Author = projectInfo?.Author ?? "Unknown",
                Type = projectInfo?.Type ?? "unknown",
                PkgPath = pkgPath,
                FileSize = fileInfo.Length,
                WorkshopId = workshopId,
                AuthorSteamId = projectInfo?.AuthorSteamId,
                Tags = projectInfo?.Tags ?? Array.Empty<string>(),
                PreviewBytes = previewBytes,
                ScanTime = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PKG 读取失败（可能损坏）: {Path}", pkgPath);

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
