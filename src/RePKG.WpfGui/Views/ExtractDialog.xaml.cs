using System.Windows;
using RePKG.WpfGui.ViewModels;

namespace RePKG.WpfGui.Views;

/// <summary>
/// ExtractDialog.xaml 的交互逻辑
/// </summary>
public partial class ExtractDialog : Window
{
    public ExtractDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExtractDialogViewModel viewModel)
        {
            viewModel.RequestClose += OnRequestClose;
        }
    }

    private void OnRequestClose(bool? dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }
}
