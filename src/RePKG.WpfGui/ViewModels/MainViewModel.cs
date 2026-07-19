using System;
using System.Collections.Generic;
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
using RePKG.WpfGui.Views;

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
    private readonly IExtractService _extractService;
    private readonly MetadataPanelViewModel _metadataPanelViewModel;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _extractCts;
    private List<WallpaperItemViewModel> _allItems = []; // 未过滤的完整列表

    public MainViewModel(
        IPkgMetadataService metadataService,
        ISteamDetectionService steamService,
        IThumbnailService thumbnailService,
        IExtractService extractService,
        MetadataPanelViewModel metadataPanelViewModel)
    {
        _metadataService = metadataService;
        _steamService = steamService;
        _thumbnailService = thumbnailService;
        _extractService = extractService;
        _metadataPanelViewModel = metadataPanelViewModel;

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

    [ObservableProperty]
    private bool _isExtracting;

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

    /// <summary>
    /// 详情面板 ViewModel
    /// </summary>
    public MetadataPanelViewModel MetadataPanel => _metadataPanelViewModel;

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

    partial void OnSelectedItemChanged(WallpaperItemViewModel? value)
    {
        _metadataPanelViewModel.UpdateItem(value);
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
        _allItems.Clear();
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
                    viewModel.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(WallpaperItemViewModel.IsSelected))
                            UpdateSelectedCount();
                    };

                    // 异步加载缩略图
                    if (item.PreviewBytes != null)
                    {
                        _ = LoadThumbnailAsync(viewModel, item.PreviewBytes, ct);
                    }

                    _allItems.Add(viewModel);
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
    /// 聚焦搜索框
    /// </summary>
    [RelayCommand]
    private void FocusSearch()
    {
        // 搜索框通过 ClearSearchText 间接聚焦
        // 实际的焦点设置在 MainWindow code-behind 中处理
        SearchBoxFocusRequested?.Invoke();
    }

    /// <summary>
    /// 搜索框聚焦请求事件（由 View 订阅）
    /// </summary>
    public event Action? SearchBoxFocusRequested;

    /// <summary>
    /// 拖拽导入扫描请求事件（由 View 订阅）
    /// </summary>
    public event Action<string>? DragDropScanRequested;

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

    /// <summary>
    /// 解包选中的壁纸
    /// </summary>
    [RelayCommand]
    private async Task ExtractSelectedAsync()
    {
        var selectedItems = WallpaperItems.Where(x => x.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            StatusText = "请先选择要解包的壁纸";
            return;
        }

        // 显示解包选项对话框
        var dialogViewModel = new ExtractDialogViewModel
        {
            SelectedCount = selectedItems.Count
        };
        var dialog = new ExtractDialog
        {
            DataContext = dialogViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true)
            return;

        var options = dialogViewModel.GetExtractOptions();

        // 创建取消令牌
        _extractCts = new CancellationTokenSource();
        var ct = _extractCts.Token;

        // 准备进度对话框
        var progressViewModel = new ExtractProgressViewModel();
        progressViewModel.TotalCount = selectedItems.Count;

        // 预填充所有进度项
        foreach (var item in selectedItems)
        {
            progressViewModel.Items.Add(new ExtractProgressItemViewModel
            {
                Title = item.Title,
                Status = "等待中",
                StatusIcon = "○",
                StatusColor = "#9E9E9E"
            });
        }

        // 取消按钮绑定
        progressViewModel.CancelRequested += () =>
        {
            _extractCts.Cancel();
        };

        var progressDialog = new ExtractProgressDialog
        {
            DataContext = progressViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        IsExtracting = true;
        StatusText = "正在解包...";

        try
        {
            // 创建进度回调
            var startTime = DateTime.UtcNow;
            int currentIndex = -1;
            var progress = new Progress<ExtractProgress>(info =>
            {
                // 更新进度条
                progressViewModel.UpdateProgress(
                    info.TotalItems,
                    info.CompletedItems,
                    info.FailedItems,
                    info.CurrentItemTitle ?? "");

                progressViewModel.UpdateElapsed(DateTime.UtcNow - startTime);

                // 更新进度列表项状态
                if (info.Status == ExtractItemStatus.Extracting)
                {
                    currentIndex++;
                    if (currentIndex < progressViewModel.Items.Count)
                    {
                        var item = progressViewModel.Items[currentIndex];
                        item.Status = "解包中...";
                        item.StatusIcon = "⟳";
                        item.StatusColor = "#0078D4";
                    }
                }
                else if (info.Status == ExtractItemStatus.Completed)
                {
                    progressViewModel.UpdateLastItemStatus(true);
                }
            });

            // 进度对话框在后台线程打开（ShowDialog 会阻塞）
            var progressDialogTask = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                progressDialog.ShowDialog();
            });

            // 执行解包
            var extractItems = selectedItems.Select(vm => vm.Model);
            var result = await _extractService.ExtractAsync(extractItems, options, progress, ct);

            // 更新失败项状态
            foreach (var error in result.Errors)
            {
                var idx = selectedItems.FindIndex(vm => vm.PkgPath == error.PkgPath);
                if (idx >= 0 && idx < progressViewModel.Items.Count)
                {
                    progressViewModel.Items[idx].Status = "失败";
                    progressViewModel.Items[idx].StatusIcon = "✗";
                    progressViewModel.Items[idx].StatusColor = "#D13438";
                }
            }

            // 更新已解包状态
            foreach (var vm in selectedItems)
            {
                if (!result.Errors.Any(e => e.PkgPath == vm.PkgPath))
                    vm.Model.IsExtracted = true;
            }

            // 标记完成
            progressViewModel.MarkCompleted(result.SuccessCount, result.FailedCount, result.Elapsed);
            StatusText = $"解包完成: 成功 {result.SuccessCount}, 失败 {result.FailedCount}";
        }
        catch (OperationCanceledException)
        {
            progressViewModel.MarkCompleted(0, 0, TimeSpan.Zero);
            progressViewModel.ResultSummary = "解包已取消";
            StatusText = "解包已取消";
        }
        catch (Exception ex)
        {
            StatusText = $"解包出错: {ex.Message}";
            progressViewModel.MarkCompleted(0, selectedItems.Count, TimeSpan.Zero);
            progressViewModel.ResultSummary = $"错误: {ex.Message}";
        }
        finally
        {
            IsExtracting = false;
            _extractCts = null;
        }
    }

    /// <summary>
    /// 解包单个壁纸（从详情面板）
    /// </summary>
    [RelayCommand]
    private async Task ExtractSingleAsync(WallpaperItemViewModel? item)
    {
        if (item == null) return;

        // 临时选中该项
        item.IsSelected = true;
        await ExtractSelectedAsync();
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
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // 无搜索词时恢复完整列表
            WallpaperItems = new ObservableCollection<WallpaperItemViewModel>(_allItems);
        }
        else
        {
            var search = SearchText.ToLowerInvariant();
            var filtered = _allItems.Where(i =>
                i.Title.ToLowerInvariant().Contains(search) ||
                i.Author.ToLowerInvariant().Contains(search) ||
                i.Tags.Any(t => t.ToLowerInvariant().Contains(search)) ||
                i.Type.ToLowerInvariant().Contains(search)
            );
            WallpaperItems = new ObservableCollection<WallpaperItemViewModel>(filtered);
        }
        OnPropertyChanged(nameof(WallpaperItems));
    }

    /// <summary>
    /// 更新选中计数
    /// </summary>
    private void UpdateSelectedCount()
    {
        SelectedCount = WallpaperItems.Count(x => x.IsSelected);
    }
}
