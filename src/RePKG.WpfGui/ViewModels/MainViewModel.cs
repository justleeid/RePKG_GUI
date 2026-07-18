using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RePKG.WpfGui.Models;
using RePKG.WpfGui.Services;

namespace RePKG.WpfGui.ViewModels;

/// <summary>
/// 视图模式
/// </summary>
public enum ViewMode
{
    /// <summary>
    /// 网格视图
    /// </summary>
    Grid,

    /// <summary>
    /// 列表视图
    /// </summary>
    List
}

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IPkgMetadataService _metadataService;
    private readonly ISteamDetectionService _steamService;
    private readonly IThumbnailService _thumbnailService;
    private CancellationTokenSource? _scanCts;

    public MainViewModel(
        IPkgMetadataService metadataService,
        ISteamDetectionService steamService,
        IThumbnailService thumbnailService)
    {
        _metadataService = metadataService;
        _steamService = steamService;
        _thumbnailService = thumbnailService;

        // 自动检测 Steam 路径
        DetectedWorkshopPath = _steamService.DetectWorkshopDirectory();
    }

    // ── 可观察属性 ──

    [ObservableProperty]
    private ObservableCollection<WallpaperItemViewModel> _wallpaperItems = [];

    [ObservableProperty]
    private WallpaperItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _scannedCount;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string? _detectedWorkshopPath;

    [ObservableProperty]
    private string _currentScanPath = string.Empty;

    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Grid;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _selectedCount;

    // ── 计算属性 ──

    /// <summary>
    /// 是否为网格视图
    /// </summary>
    public bool IsGridView => CurrentViewMode == ViewMode.Grid;

    /// <summary>
    /// 是否为列表视图
    /// </summary>
    public bool IsListView => CurrentViewMode == ViewMode.List;

    /// <summary>
    /// 视图切换按钮文本
    /// </summary>
    public string ViewModeToggleText => CurrentViewMode == ViewMode.Grid ? "☰ 列表" : "⊞ 网格";

    // ── 属性变更处理 ──

    partial void OnCurrentViewModeChanged(ViewMode value)
    {
        OnPropertyChanged(nameof(IsGridView));
        OnPropertyChanged(nameof(IsListView));
        OnPropertyChanged(nameof(ViewModeToggleText));
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySearchFilter();
    }

    // ── 命令 ──

    /// <summary>
    /// 打开目录对话框
    /// </summary>
    [RelayCommand]
    private async Task OpenDirectoryAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择 Wallpaper Engine Workshop 目录"
        };

        if (!string.IsNullOrEmpty(DetectedWorkshopPath))
            dialog.InitialDirectory = DetectedWorkshopPath;

        if (dialog.ShowDialog() == true)
        {
            await ScanDirectoryAsync(dialog.FolderName);
        }
    }

    /// <summary>
    /// 扫描检测到的 Workshop 目录
    /// </summary>
    [RelayCommand]
    private async Task ScanDetectedDirectoryAsync()
    {
        if (!string.IsNullOrEmpty(DetectedWorkshopPath))
        {
            await ScanDirectoryAsync(DetectedWorkshopPath);
        }
    }

    /// <summary>
    /// 扫描指定目录
    /// </summary>
    [RelayCommand]
    private async Task ScanDirectoryAsync(string directoryPath)
    {
        if (IsScanning || string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            return;

        // 取消之前的扫描
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        IsScanning = true;
        CurrentScanPath = directoryPath;
        WallpaperItems.Clear();
        TotalCount = 0;
        ScannedCount = 0;
        StatusText = "正在扫描...";

        try
        {
            // 递归查找所有 .pkg 文件
            var pkgFiles = Directory.GetFiles(directoryPath, "*.pkg", SearchOption.AllDirectories);
            TotalCount = pkgFiles.Length;
            StatusText = $"发现 {TotalCount} 个 PKG 文件，正在提取元数据...";

            // 逐个提取元数据
            foreach (var pkgPath in pkgFiles)
            {
                ct.ThrowIfCancellationRequested();

                var item = await _metadataService.ExtractMetadataAsync(pkgPath, ct);
                if (item != null)
                {
                    var viewModel = new WallpaperItemViewModel(item);

                    // 异步加载缩略图
                    if (item.PreviewBytes != null)
                    {
                        _ = LoadThumbnailAsync(viewModel, item.PreviewBytes, ct);
                    }

                    WallpaperItems.Add(viewModel);
                }

                ScannedCount++;
                StatusText = $"正在扫描... {ScannedCount}/{TotalCount}";
            }

            StatusText = $"扫描完成，共 {WallpaperItems.Count} 个壁纸";
        }
        catch (OperationCanceledException)
        {
            StatusText = "扫描已取消";
        }
        catch (Exception ex)
        {
            StatusText = $"扫描出错: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>
    /// 取消扫描
    /// </summary>
    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
    }

    /// <summary>
    /// 刷新列表
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(CurrentScanPath) && Directory.Exists(CurrentScanPath))
        {
            await ScanDirectoryAsync(CurrentScanPath);
        }
    }

    /// <summary>
    /// 切换视图模式
    /// </summary>
    [RelayCommand]
    private void ToggleViewMode()
    {
        CurrentViewMode = CurrentViewMode == ViewMode.Grid ? ViewMode.List : ViewMode.Grid;
    }

    /// <summary>
    /// 全选
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in WallpaperItems)
        {
            item.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    /// <summary>
    /// 取消全选
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in WallpaperItems)
        {
            item.IsSelected = false;
        }
        UpdateSelectedCount();
    }

    // ── 私有方法 ──

    /// <summary>
    /// 异步加载缩略图
    /// </summary>
    private async Task LoadThumbnailAsync(WallpaperItemViewModel viewModel, byte[] previewBytes, CancellationToken ct)
    {
        try
        {
            viewModel.IsLoadingPreview = true;
            var thumbnail = await _thumbnailService.CreateThumbnailAsync(previewBytes, 200);
            if (!ct.IsCancellationRequested)
            {
                viewModel.PreviewImage = thumbnail;
            }
        }
        catch
        {
            // 缩略图加载失败不影响主流程
        }
        finally
        {
            viewModel.IsLoadingPreview = false;
        }
    }

    /// <summary>
    /// 应用搜索过滤
    /// </summary>
    private void ApplySearchFilter()
    {
        // TODO: 实现搜索过滤逻辑
    }

    /// <summary>
    /// 更新选中计数
    /// </summary>
    private void UpdateSelectedCount()
    {
        SelectedCount = WallpaperItems.Count(x => x.IsSelected);
    }
}
