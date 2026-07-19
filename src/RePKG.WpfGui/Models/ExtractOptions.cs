namespace RePKG.WpfGui.Models;

/// <summary>
/// 解包配置选项
/// </summary>
public class ExtractOptions
{
    /// <summary>
    /// 输出目录
    /// </summary>
    public string OutputDirectory { get; set; } = "./output";

    /// <summary>
    /// 使用壁纸名称作为子文件夹（对应 -n 参数）
    /// </summary>
    public bool UseNameAsFolder { get; set; } = true;

    /// <summary>
    /// 所有文件放在同一个目录下（对应 -s 参数）
    /// </summary>
    public bool SingleDirectory { get; set; }

    /// <summary>
    /// 转换 TEX 为图片（对应 --no-tex-convert 取反）
    /// </summary>
    public bool ConvertTexToImage { get; set; } = true;

    /// <summary>
    /// 覆盖已存在的文件（对应 --overwrite）
    /// </summary>
    public bool OverwriteExisting { get; set; }

    /// <summary>
    /// 复制项目文件（对应 -c）
    /// </summary>
    public bool CopyProjectFiles { get; set; } = true;

    /// <summary>
    /// 仅提取指定扩展名（对应 -e），如 [".png", ".jpg"]
    /// </summary>
    public string[]? IncludeExtensions { get; set; }

    /// <summary>
    /// 忽略指定扩展名（对应 -i），如 [".wav", ".mp3"]
    /// </summary>
    public string[]? ExcludeExtensions { get; set; }
}
