using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RePKG.WpfGui.Models;

namespace RePKG.WpfGui.Services;

/// <summary>
/// 解包服务接口
/// </summary>
public interface IExtractService
{
    /// <summary>
    /// 批量解包壁纸
    /// </summary>
    /// <param name="items">待解包列表</param>
    /// <param name="options">解包配置</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解包结果</returns>
    Task<ExtractResult> ExtractAsync(
        IEnumerable<WallpaperItem> items,
        ExtractOptions options,
        IProgress<ExtractProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 解包单个壁纸
    /// </summary>
    /// <param name="item">壁纸项</param>
    /// <param name="options">解包配置</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> ExtractSingleAsync(
        WallpaperItem item,
        ExtractOptions options,
        IProgress<ExtractProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
