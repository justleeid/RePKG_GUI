using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RePKG.WpfGui.Services;

/// <summary>
/// 缩略图服务实现
/// </summary>
public class ThumbnailService : IThumbnailService
{
    /// <inheritdoc/>
    public BitmapImage? CreateThumbnail(byte[] imageBytes, int decodePixelWidth = 200)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = decodePixelWidth;
            bitmap.StreamSource = new MemoryStream(imageBytes);
            bitmap.EndInit();
            bitmap.Freeze(); // 冻结以支持跨线程访问
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<BitmapImage?> CreateThumbnailAsync(byte[] imageBytes, int decodePixelWidth = 200)
    {
        return await Task.Run(() => CreateThumbnail(imageBytes, decodePixelWidth));
    }
}
