using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RePKG.WpfGui.Services;

/// <summary>
/// 缩略图服务接口
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// 将图片字节数据转换为 BitmapImage
    /// </summary>
    /// <param name="imageBytes">图片字节数据</param>
    /// <param name="decodePixelWidth">解码宽度（用于缩放）</param>
    /// <returns>BitmapImage，失败返回 null</returns>
    BitmapImage? CreateThumbnail(byte[] imageBytes, int decodePixelWidth = 200);

    /// <summary>
    /// 异步创建缩略图
    /// </summary>
    /// <param name="imageBytes">图片字节数据</param>
    /// <param name="decodePixelWidth">解码宽度</param>
    /// <returns>BitmapImage，失败返回 null</returns>
    Task<BitmapImage?> CreateThumbnailAsync(byte[] imageBytes, int decodePixelWidth = 200);

    /// <summary>
    /// 获取磁盘缓存大小（字节）
    /// </summary>
    long GetCacheSizeBytes();

    /// <summary>
    /// 清理磁盘缓存
    /// </summary>
    void ClearCache();
}
