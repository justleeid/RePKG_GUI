using System.IO;
using System.Windows;
using System.Windows.Controls;
using RePKG.WpfGui.ViewModels;

namespace RePKG.WpfGui.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
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
}
