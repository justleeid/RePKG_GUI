using System;

namespace RePKG.WpfGui.Models;

/// <summary>
/// 壁纸列表项数据模型
/// </summary>
public class WallpaperItem
{
    /// <summary>
    /// 壁纸标题（来自 project.json.title）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 作者（来自 project.json.author）
    /// </summary>
    public string Author { get; set; } = "Unknown";

    /// <summary>
    /// 壁纸类型：scene / web / video / application
    /// </summary>
    public string Type { get; set; } = "unknown";

    /// <summary>
    /// PKG 文件本地路径
    /// </summary>
    public string PkgPath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Workshop ID
    /// </summary>
    public string WorkshopId { get; set; } = string.Empty;

    /// <summary>
    /// 标签列表
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 缩略图数据（preview.jpg 的原始字节）
    /// </summary>
    public byte[]? PreviewBytes { get; set; }

    /// <summary>
    /// 元数据提取时间
    /// </summary>
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已解包
    /// </summary>
    public bool IsExtracted { get; set; }

    /// <summary>
    /// 扫描是否失败
    /// </summary>
    public bool IsScanFailed { get; set; }

    /// <summary>
    /// 失败原因（如果扫描失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
    public string FileSizeDisplay => FormatFileSize(FileSize);

    /// <summary>
    /// 类型图标
    /// </summary>
    public string TypeIcon => Type switch
    {
        "scene" => "🎬",
        "web" => "🌍",
        "video" => "📹",
        "application" => "📱",
        _ => "📄"
    };

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.#} {suffixes[order]}";
    }
}
