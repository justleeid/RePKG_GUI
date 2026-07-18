using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RePKG.WpfGui.Models;

namespace RePKG.WpfGui.ViewModels;

/// <summary>
/// 解包选项对话框 ViewModel
/// </summary>
public partial class ExtractDialogViewModel : ObservableObject
{
    /// <summary>
    /// 请求关闭对话框事件
    /// </summary>
    public event Action<bool?>? RequestClose;

    [ObservableProperty]
    private string _outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output");

    [ObservableProperty]
    private bool _useNameAsFolder = true;

    [ObservableProperty]
    private bool _convertTexToImage = true;

    [ObservableProperty]
    private bool _overwriteExisting;

    [ObservableProperty]
    private bool _copyProjectFiles = true;

    [ObservableProperty]
    private bool _filterImagesOnly;

    [ObservableProperty]
    private bool _filterModelsOnly;

    [ObservableProperty]
    private bool _excludeAudio;

    [ObservableProperty]
    private int _selectedCount;

    /// <summary>
    /// 获取配置的解包选项
    /// </summary>
    public ExtractOptions GetExtractOptions()
    {
        var options = new ExtractOptions
        {
            OutputDirectory = OutputDirectory,
            UseNameAsFolder = UseNameAsFolder,
            ConvertTexToImage = ConvertTexToImage,
            OverwriteExisting = OverwriteExisting,
            CopyProjectFiles = CopyProjectFiles
        };

        // 应用文件过滤
        if (FilterImagesOnly)
        {
            options.IncludeExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga"];
        }
        else if (FilterModelsOnly)
        {
            options.IncludeExtensions = [".obj", ".fbx", ".gltf", ".glb"];
        }

        if (ExcludeAudio)
        {
            options.ExcludeExtensions = [".wav", ".mp3", ".ogg", ".flac"];
        }

        return options;
    }

    [RelayCommand]
    private void BrowseOutputDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择输出目录"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputDirectory = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
