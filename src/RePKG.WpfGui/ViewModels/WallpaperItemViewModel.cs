using System;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using RePKG.WpfGui.Models;

namespace RePKG.WpfGui.ViewModels;

/// <summary>
/// 壁纸列表项 ViewModel
/// </summary>
public partial class WallpaperItemViewModel : ObservableObject
{
    private readonly WallpaperItem _model;

    public WallpaperItemViewModel(WallpaperItem model)
    {
        _model = model;
        _title = model.Title;
        _author = model.Author;
        _type = model.Type;
        _fileSize = model.FileSize;
        _fileSizeDisplay = model.FileSizeDisplay;
        _workshopId = model.WorkshopId;
        _tags = model.Tags;
        _typeIcon = model.TypeIcon;
        _isScanFailed = model.IsScanFailed;
        _errorMessage = model.ErrorMessage;
        AuthorSteamId = model.AuthorSteamId;
    }

    // ── 可观察属性 ──

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _author;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private long _fileSize;

    [ObservableProperty]
    private string _fileSizeDisplay;

    [ObservableProperty]
    private string _workshopId;

    [ObservableProperty]
    private string[] _tags;

    [ObservableProperty]
    private string _typeIcon;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isScanFailed;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private bool _isLoadingPreview;

    // ── 只读属性 ──

    /// <summary>
    /// PKG 文件路径
    /// </summary>
    public string PkgPath => _model.PkgPath;

    /// <summary>
    /// 作者 Steam ID
    /// </summary>
    public string? AuthorSteamId { get; private set; }

    /// <summary>
    /// 原始缩略图字节数据
    /// </summary>
    public byte[]? PreviewBytes => _model.PreviewBytes;

    /// <summary>
    /// 底层数据模型引用（供解包等操作使用）
    /// </summary>
    public WallpaperItem Model => _model;

    /// <summary>
    /// 标签显示文本
    /// </summary>
    public string TagsDisplay => string.Join(", ", Tags);

    /// <summary>
    /// 状态颜色（扫描失败为红色）
    /// </summary>
    public string StatusColor => IsScanFailed ? "#D13438" : "#107C10";

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => IsScanFailed ? "扫描失败" : "就绪";
}
