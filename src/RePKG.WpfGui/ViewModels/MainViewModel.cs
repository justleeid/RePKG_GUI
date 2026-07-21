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
/// 排序字段
/// </summary>
public enum SortField
{
    Title,
    Author,
    FileSize,
    Type
}

/// <summary>
/// 分类项
/// </summary>
public partial class CategoryItem : ObservableObject
{
    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _filterKey = string.Empty;

    [ObservableProperty]
    private int _count;
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
    private bool _isUpdatingFilter; // 防止递归更新
    private int _scannedCount; // 支持 Interlocked

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

        // 初始化缓存显示
        RefreshCacheDisplay();

        // 加载保存的主题
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (config?.IsDarkMode == true)
                    IsDarkMode = true;
            }
            catch { }
        }
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

    /// <summary>
    /// 已扫描数量（支持并发更新）
    /// </summary>
    public int ScannedCount
    {
        get => _scannedCount;
        set
        {
            _scannedCount = value;
            OnPropertyChanged();
        }
    }

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

    [ObservableProperty]
    private SortField _currentSortField = SortField.Title;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private string? _selectedType = "全部类型";

    [ObservableProperty]
    private string? _selectedTag = "全部标签";

    [ObservableProperty]
    private bool _showSelectedOnly;

    [ObservableProperty]
    private string _cacheDisplay = "缓存: 计算中...";

    [ObservableProperty]
    private bool _isDarkMode;

    /// <summary>
    /// 主题切换按钮文本
    /// </summary>
    public string ThemeToggleText => IsDarkMode ? "☀️" : "🌙";

    [ObservableProperty]
    private CategoryItem? _selectedCategory;

    /// <summary>
    /// 分类列表（左侧导航）
    /// </summary>
    public ObservableCollection<CategoryItem> Categories { get; } =
    [
        new() { Icon = "📁", Name = "全部", FilterKey = "all" },
        new() { Icon = "🎬", Name = "Scene", FilterKey = "scene" },
        new() { Icon = "🌍", Name = "Web", FilterKey = "web" },
        new() { Icon = "📹", Name = "Video", FilterKey = "video" },
        new() { Icon = "📱", Name = "Application", FilterKey = "application" }
    ];

    /// <summary>
    /// 可用的壁纸类型列表（扫描后自动填充）
    /// </summary>
    public ObservableCollection<string> AvailableTypes { get; } = ["全部类型"];

    /// <summary>
    /// 可用的标签列表（扫描后自动填充）
    /// </summary>
    public ObservableCollection<string> AvailableTags { get; } = ["全部标签"];

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
    /// 排序方向按钮文本
    /// </summary>
    public string SortDirectionText => SortAscending ? "↑ 升序" : "↓ 降序";

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

    partial void OnSelectedCategoryChanged(CategoryItem? value)
    {
        ApplySearchFilter();
    }

    partial void OnCurrentSortFieldChanged(SortField value)
    {
        ApplySortToAllItems();
        ApplySearchFilter();
    }

    partial void OnSortAscendingChanged(bool value)
    {
        OnPropertyChanged(nameof(SortDirectionText));
        ApplySortToAllItems();
        ApplySearchFilter();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        OnPropertyChanged(nameof(ThemeToggleText));
        App.ApplyTheme(value);
    }

    partial void OnSelectedTypeChanged(string? value)
    {
        ApplySearchFilter();
    }

    partial void OnSelectedTagChanged(string? value)
    {
        ApplySearchFilter();
    }

    partial void OnShowSelectedOnlyChanged(bool value)
    {
        ApplySearchFilter();
    }

    // ── 命令 ──

    /// <summary>
    /// 切换深色/浅色主题
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }

    /// <summary>
    /// 刷新缓存显示
    /// </summary>
    private void RefreshCacheDisplay()
    {
        try
        {
            var bytes = _thumbnailService.GetCacheSizeBytes();
            CacheDisplay = bytes switch
            {
                < 1024 => "缓存: 0 MB",
                < 1024 * 1024 => $"缓存: {bytes / 1024:N0} KB",
                _ => $"缓存: {bytes / (1024 * 1024):N0} MB"
            };
        }
        catch
        {
            CacheDisplay = "缓存: --";
        }
    }

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
    /// 扫描指定目录（带并发控制）
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

            // 并发控制：限制同时处理的 PKG 数量
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
            var tasks = pkgFiles.Select(async pkgPath =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
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

                        // 在 UI 线程添加到集合
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _allItems.Add(viewModel);
                            WallpaperItems.Add(viewModel);
                        });
                    }

                    Interlocked.Increment(ref _scannedCount);
                    StatusText = $"正在扫描... {ScannedCount}/{TotalCount}";
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            // 扫描完成后按当前排序字段排序
            ApplySortToAllItems();
            ApplySearchFilter();

            // 更新分类计数
            UpdateCategoryCounts();

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
    /// 切换排序方向
    /// </summary>
    [RelayCommand]
    private void ToggleSortDirection()
    {
        SortAscending = !SortAscending;
    }

    /// <summary>
    /// 设置排序字段
    /// </summary>
    [RelayCommand]
    private void SetSortField(string field)
    {
        if (Enum.TryParse<SortField>(field, out var sortField))
        {
            if (CurrentSortField == sortField)
                SortAscending = !SortAscending;
            else
            {
                CurrentSortField = sortField;
                SortAscending = true;
            }
        }
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
    /// 按类型全选（选中当前筛选结果中所有项）
    /// </summary>
    [RelayCommand]
    private void SelectByType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return;
        foreach (var item in WallpaperItems.Where(i => i.Type == type))
        {
            item.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    /// <summary>
    /// 按标签全选（选中包含指定标签的所有项）
    /// </summary>
    [RelayCommand]
    private void SelectByTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        foreach (var item in WallpaperItems.Where(i => i.Tags.Any(t => t == tag)))
        {
            item.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    /// <summary>
    /// 清除所有筛选条件
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedType = "全部类型";
        SelectedTag = "全部标签";
        ShowSelectedOnly = false;
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
            var progress = new Progress<ExtractProgress>(info =>
            {
                // 更新进度条
                progressViewModel.UpdateProgress(
                    info.TotalItems,
                    info.CompletedItems,
                    info.FailedItems,
                    info.CurrentItemTitle ?? "");

                progressViewModel.UpdateElapsed(DateTime.UtcNow - startTime);

                // 更新文件级进度
                if (info.TotalFiles > 0 && info.Status == ExtractItemStatus.Extracting)
                {
                    progressViewModel.FileProgressText = $"文件: {info.ProcessedFiles}/{info.TotalFiles}  {info.CurrentFileName}";
                }
                else if (info.Status == ExtractItemStatus.Completed)
                {
                    progressViewModel.FileProgressText = "";
                }

                // 更新进度列表项状态
                var itemIndex = info.CompletedItems;
                if (info.Status == ExtractItemStatus.Extracting)
                {
                    // 当前正在处理的项
                    if (itemIndex < progressViewModel.Items.Count)
                    {
                        var item = progressViewModel.Items[itemIndex];
                        item.Status = "解包中...";
                        item.StatusIcon = "⟳";
                        item.StatusColor = "#0078D4";
                    }
                }
                else if (info.Status == ExtractItemStatus.Completed)
                {
                    // 刚刚完成的项（CompletedItems 已经 +1）
                    var completedIndex = info.CompletedItems - 1;
                    if (completedIndex >= 0 && completedIndex < progressViewModel.Items.Count)
                    {
                        var item = progressViewModel.Items[completedIndex];
                        item.Status = "完成";
                        item.StatusIcon = "✓";
                        item.StatusColor = "#107C10";
                    }
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
    /// 解包单个壁纸（从详情面板，不影响其他选中项）
    /// </summary>
    [RelayCommand]
    private async Task ExtractSingleAsync(WallpaperItemViewModel? item)
    {
        if (item == null) return;

        // 显示解包选项对话框
        var dialogViewModel = new ExtractDialogViewModel
        {
            SelectedCount = 1
        };
        var dialog = new ExtractDialog
        {
            DataContext = dialogViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true)
            return;

        var options = dialogViewModel.GetExtractOptions();
        _extractCts = new CancellationTokenSource();

        IsExtracting = true;
        StatusText = $"正在解包: {item.Title}";

        try
        {
            var result = await _extractService.ExtractAsync(
                [item.Model], options, null, _extractCts.Token);

            if (result.SuccessCount > 0)
                item.Model.IsExtracted = true;

            StatusText = $"解包完成: {item.Title}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "解包已取消";
        }
        catch (Exception ex)
        {
            StatusText = $"解包出错: {ex.Message}";
        }
        finally
        {
            IsExtracting = false;
            _extractCts = null;
        }
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
    /// 应用搜索过滤和排序（增量更新，避免 UI 闪烁）
    /// </summary>
    private void ApplySearchFilter()
    {
        if (_isUpdatingFilter) return;
        _isUpdatingFilter = true;

        try
        {
            // 过滤
            IEnumerable<WallpaperItemViewModel> filtered = _allItems;

            // 左侧导航分类筛选
            if (SelectedCategory != null && SelectedCategory.FilterKey != "all")
            {
                filtered = filtered.Where(i =>
                    string.Equals(i.Type, SelectedCategory.FilterKey, StringComparison.OrdinalIgnoreCase));
            }

            // 搜索文本过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(i =>
                    i.Title.ToLowerInvariant().Contains(search) ||
                    i.Author.ToLowerInvariant().Contains(search) ||
                    i.Tags.Any(t => t.ToLowerInvariant().Contains(search)) ||
                    i.Type.ToLowerInvariant().Contains(search));
            }

            // 仅显示已选中项
            if (ShowSelectedOnly)
            {
                filtered = filtered.Where(i => i.IsSelected);
            }

            // 排序
            filtered = CurrentSortField switch
            {
                SortField.Title => SortAscending
                    ? filtered.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
                    : filtered.OrderByDescending(i => i.Title, StringComparer.OrdinalIgnoreCase),
                SortField.Author => SortAscending
                    ? filtered.OrderBy(i => i.Author, StringComparer.OrdinalIgnoreCase)
                    : filtered.OrderByDescending(i => i.Author, StringComparer.OrdinalIgnoreCase),
                SortField.FileSize => SortAscending
                    ? filtered.OrderBy(i => i.FileSize)
                    : filtered.OrderByDescending(i => i.FileSize),
                SortField.Type => SortAscending
                    ? filtered.OrderBy(i => i.Type)
                    : filtered.OrderByDescending(i => i.Type),
                _ => filtered
            };

            var filteredList = filtered.ToList();

            // 增量更新：对比差异，避免重建整个集合
            WallpaperItems.Clear();
            foreach (var item in filteredList)
            {
                WallpaperItems.Add(item);
            }
        }
        finally
        {
            _isUpdatingFilter = false;
        }
    }

    /// <summary>
    /// 对 _allItems 应用排序（扫描完成后调用）
    /// </summary>
    private void ApplySortToAllItems()
    {
        var sorted = CurrentSortField switch
        {
            SortField.Title => SortAscending
                ? _allItems.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase).ToList()
                : _allItems.OrderByDescending(i => i.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            SortField.Author => SortAscending
                ? _allItems.OrderBy(i => i.Author, StringComparer.OrdinalIgnoreCase).ToList()
                : _allItems.OrderByDescending(i => i.Author, StringComparer.OrdinalIgnoreCase).ToList(),
            SortField.FileSize => SortAscending
                ? _allItems.OrderBy(i => i.FileSize).ToList()
                : _allItems.OrderByDescending(i => i.FileSize).ToList(),
            SortField.Type => SortAscending
                ? _allItems.OrderBy(i => i.Type).ToList()
                : _allItems.OrderByDescending(i => i.Type).ToList(),
            _ => _allItems.ToList()
        };

        _allItems.Clear();
        _allItems.AddRange(sorted);
    }

    /// <summary>
    /// 更新分类计数
    /// </summary>
    private void UpdateCategoryCounts()
    {
        var typeCounts = _allItems
            .GroupBy(i => i.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var category in Categories)
        {
            if (category.FilterKey == "all")
            {
                category.Count = _allItems.Count;
            }
            else if (typeCounts.TryGetValue(category.FilterKey, out var count))
            {
                category.Count = count;
            }
            else
            {
                category.Count = 0;
            }
        }
    }

    /// <summary>
    /// 更新选中计数
    /// </summary>
    private void UpdateSelectedCount()
    {
        SelectedCount = WallpaperItems.Count(x => x.IsSelected);
    }

    /// <summary>
    /// 类型中文名映射
    /// </summary>
    private static readonly Dictionary<string, string> TypeDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["scene"] = "scene（场景）",
        ["video"] = "video（视频）",
        ["web"] = "web（网页）",
        ["application"] = "application（应用）",
    };

    /// <summary>
    /// 扫描完成后更新筛选选项（类型列表、标签列表）
    /// </summary>
    private void UpdateFilterOptions()
    {
        // 收集所有出现过的类型（忽略大小写去重，显示中文名）
        var types = _allItems
            .Select(i => i.Type?.ToLowerInvariant())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        AvailableTypes.Clear();
        AvailableTypes.Add("全部类型");
        foreach (var type in types)
            AvailableTypes.Add(TypeDisplayNames.TryGetValue(type!, out var display) ? display : type!);

        // 收集所有出现过的标签（忽略大小写去重，显示为小写）
        var tags = _allItems
            .SelectMany(i => i.Tags)
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        AvailableTags.Clear();
        AvailableTags.Add("全部标签");
        foreach (var tag in tags)
            AvailableTags.Add(tag!);
    }
}
