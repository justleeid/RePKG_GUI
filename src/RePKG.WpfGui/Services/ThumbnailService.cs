using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RePKG.WpfGui.Services;

/// <summary>
/// 缩略图服务实现（内存 LRU + 磁盘缓存）
/// </summary>
public class ThumbnailService : IThumbnailService, IDisposable
{
    private const int MaxMemoryItems = 500;
    private const long MaxDiskCacheBytes = 500 * 1024 * 1024; // 500MB
    private static readonly string DiskCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RePKG.Gui", "Thumbs");

    // L1: 内存缓存（线程安全字典 + 访问顺序追踪）
    private readonly ConcurrentDictionary<string, (BitmapImage Image, DateTime LastAccess)> _memoryCache = new();
    private readonly object _memoryLock = new();

    public ThumbnailService()
    {
        Directory.CreateDirectory(DiskCacheDir);
    }

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
            bitmap.Freeze();
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
        if (imageBytes == null || imageBytes.Length == 0)
            return null;

        // 计算缓存 Key（基于内容哈希）
        var cacheKey = ComputeCacheKey(imageBytes);

        // L1: 检查内存缓存
        if (_memoryCache.TryGetValue(cacheKey, out var cached))
        {
            lock (_memoryLock)
            {
                _memoryCache[cacheKey] = (cached.Image, DateTime.UtcNow);
            }
            return cached.Image;
        }

        // L2: 检查磁盘缓存
        var diskPath = Path.Combine(DiskCacheDir, $"{cacheKey}.jpg");
        if (File.Exists(diskPath))
        {
            var diskBytes = await File.ReadAllBytesAsync(diskPath);
            var image = CreateThumbnail(diskBytes, decodePixelWidth);
            if (image != null)
            {
                AddToMemoryCache(cacheKey, image);
                return image;
            }
        }

        // 缓存未命中：解码并缓存
        var thumbnail = await Task.Run(() => CreateThumbnail(imageBytes, decodePixelWidth));
        if (thumbnail != null)
        {
            AddToMemoryCache(cacheKey, thumbnail);
            await SaveToDiskCacheAsync(cacheKey, imageBytes);
        }

        return thumbnail;
    }

    private void AddToMemoryCache(string key, BitmapImage image)
    {
        lock (_memoryLock)
        {
            // 超出容量时淘汰最旧的
            if (_memoryCache.Count >= MaxMemoryItems)
            {
                var oldest = _memoryCache.MinBy(x => x.Value.LastAccess);
                _memoryCache.TryRemove(oldest.Key, out _);
            }
            _memoryCache[key] = (image, DateTime.UtcNow);
        }
    }

    private async Task SaveToDiskCacheAsync(string key, byte[] imageBytes)
    {
        try
        {
            var diskPath = Path.Combine(DiskCacheDir, $"{key}.jpg");
            await File.WriteAllBytesAsync(diskPath, imageBytes);

            // 异步清理磁盘缓存
            await Task.Run(CleanupDiskCache);
        }
        catch
        {
            // 磁盘缓存写入失败不影响主流程
        }
    }

    private static void CleanupDiskCache()
    {
        try
        {
            var files = new DirectoryInfo(DiskCacheDir).GetFiles("*.jpg");
            var totalSize = files.Sum(f => f.Length);

            if (totalSize <= MaxDiskCacheBytes)
                return;

            // 按最后访问时间排序，删除最旧的
            var sortedFiles = files.OrderBy(f => f.LastAccessTimeUtc).ToList();
            foreach (var file in sortedFiles)
            {
                if (totalSize <= MaxDiskCacheBytes * 0.8)
                    break;

                totalSize -= file.Length;
                file.Delete();
            }
        }
        catch
        {
            // 清理失败不影响主流程
        }
    }

    private static string ComputeCacheKey(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16]; // 取前16位作为文件名
    }

    public void Dispose()
    {
        _memoryCache.Clear();
    }
}
