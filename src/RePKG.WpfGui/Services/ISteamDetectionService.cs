namespace RePKG.WpfGui.Services;

/// <summary>
/// Steam 路径检测服务接口
/// </summary>
public interface ISteamDetectionService
{
    /// <summary>
    /// 检测 Steam 安装路径
    /// </summary>
    /// <returns>Steam 安装路径，未找到返回 null</returns>
    string? DetectSteamPath();

    /// <summary>
    /// 获取 Workshop 内容目录
    /// </summary>
    /// <param name="steamPath">Steam 安装路径</param>
    /// <returns>Workshop 内容目录路径</returns>
    string GetWorkshopPath(string steamPath);

    /// <summary>
    /// 自动检测 Workshop 目录（包含 431960 = Wallpaper Engine）
    /// </summary>
    /// <returns>Workshop 目录路径，未找到返回 null</returns>
    string? DetectWorkshopDirectory();
}
