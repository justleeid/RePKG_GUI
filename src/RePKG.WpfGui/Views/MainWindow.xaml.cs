using System.IO;
using System.Windows;
using System.Windows.Controls;
using RePKG.WpfGui.ViewModels;

namespace RePKG.WpfGui.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            vm.SearchBoxFocusRequested += () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    var searchBox = FindName("SearchBox") as TextBox;
                    searchBox?.Focus();
                });
            };
        }
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths?.Length == 1 && Directory.Exists(paths[0]))
            {
                e.Effects = DragDropEffects.Copy;
                DragOverlay.Visibility = Visibility.Visible;
            }
        }
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        DragOverlay.Visibility = Visibility.Collapsed;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        DragOverlay.Visibility = Visibility.Collapsed;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths?.Length > 0 && Directory.Exists(paths[0]))
            {
                if (DataContext is MainViewModel vm)
                {
                    _ = vm.ScanDirectoryCommand.ExecuteAsync(paths[0]);
                }
            }
        }
        e.Handled = true;
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "RePKG GUI v0.1.0\n\n" +
            "Wallpaper Engine PKG/TEX 解包工具图形界面\n\n" +
            "基于 RePKG (https://github.com/notscuffed/repkg)",
            "关于 RePKG GUI",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
}
