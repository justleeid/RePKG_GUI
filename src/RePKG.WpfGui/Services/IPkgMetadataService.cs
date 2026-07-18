using System.Threading;
using System.Threading.Tasks;
using RePKG.WpfGui.Models;

namespace RePKG.WpfGui.Services;

/// <summary>
/// PKG 元数据提取服务接口
/// </summary>
public interface IPkgMetadataService
{
    /// <summary>
    /// 从 PKG 文件提取元数据
    /// </summary>
    /// <param name="pkgPath">PKG 文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>壁纸数据模型，失败返回 null</returns>
    Task<WallpaperItem?> ExtractMetadataAsync(string pkgPath, CancellationToken cancellationToken = default);
}
