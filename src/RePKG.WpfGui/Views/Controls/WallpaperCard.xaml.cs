using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RePKG.WpfGui.ViewModels;

namespace RePKG.WpfGui.Views.Controls;

/// <summary>
/// WallpaperCard.xaml 的交互逻辑
/// </summary>
public partial class WallpaperCard : UserControl
{
    public WallpaperCard()
    {
        InitializeComponent();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is WallpaperItemViewModel viewModel)
        {
            viewModel.IsSelected = !viewModel.IsSelected;
            e.Handled = true;
        }
    }
}
