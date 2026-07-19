using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RePKG.WpfGui.ViewModels;

/// <summary>
/// 详情面板 ViewModel
/// </summary>
public partial class MetadataPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private WallpaperItemViewModel? _item;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _typeIcon = string.Empty;

    [ObservableProperty]
    private string _fileSizeDisplay = string.Empty;

    [ObservableProperty]
    private string _workshopId = string.Empty;

    [ObservableProperty]
    private string _pkgPath = string.Empty;

    [ObservableProperty]
    private string _tagsDisplay = string.Empty;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private string _workshopUrl = string.Empty;

    [ObservableProperty]
    private string? _authorSteamId;

    [ObservableProperty]
    private string _authorSteamUrl = string.Empty;

    [ObservableProperty]
    private bool _hasWorkshopId;

    [ObservableProperty]
    private bool _hasAuthorSteamId;

    /// <summary>
    /// 更新显示的壁纸项
    /// </summary>
    public void UpdateItem(WallpaperItemViewModel? item)
    {
        Item = item;

        if (item == null)
        {
            IsVisible = false;
            return;
        }

        Title = item.Title;
        Author = item.Author;
        Type = item.Type;
        TypeIcon = item.TypeIcon;
        FileSizeDisplay = item.FileSizeDisplay;
        WorkshopId = item.WorkshopId;
        PkgPath = item.PkgPath;
        TagsDisplay = item.TagsDisplay;
        PreviewImage = item.PreviewImage;
        HasWorkshopId = !string.IsNullOrEmpty(item.WorkshopId);
        WorkshopUrl = HasWorkshopId
            ? $"https://steamcommunity.com/sharedfiles/filedetails/?id={item.WorkshopId}"
            : string.Empty;
        AuthorSteamId = item.AuthorSteamId;
        HasAuthorSteamId = !string.IsNullOrEmpty(item.AuthorSteamId);
        AuthorSteamUrl = HasAuthorSteamId
            ? $"https://steamcommunity.com/profiles/{item.AuthorSteamId}"
            : string.Empty;
        IsVisible = true;
    }

    [RelayCommand]
    private void OpenAuthorSteamUrl()
    {
        if (!string.IsNullOrEmpty(AuthorSteamUrl))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = AuthorSteamUrl,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    [RelayCommand]
    private void OpenWorkshopUrl()
    {
        if (!string.IsNullOrEmpty(WorkshopUrl))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = WorkshopUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // 忽略打开链接失败
            }
        }
    }

    [RelayCommand]
    private void OpenContainingFolder()
    {
        if (!string.IsNullOrEmpty(PkgPath) && System.IO.File.Exists(PkgPath))
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{PkgPath}\"");
            }
            catch
            {
                // 忽略打开文件夹失败
            }
        }
    }

    [RelayCommand]
    private void Close()
    {
        IsVisible = false;
    }
}
