using System.Windows;
using RePKG.WpfGui.ViewModels;

namespace RePKG.WpfGui.Views;

/// <summary>
/// ExtractProgressDialog.xaml 的交互逻辑
/// </summary>
public partial class ExtractProgressDialog : Window
{
    public ExtractProgressDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExtractProgressViewModel viewModel)
        {
            viewModel.RequestClose += () =>
            {
                Close();
            };
        }
    }
}
