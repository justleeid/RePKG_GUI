using System.Windows;

namespace RePKG.WpfGui.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
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
