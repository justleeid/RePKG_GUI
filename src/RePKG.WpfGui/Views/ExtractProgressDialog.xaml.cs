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

            viewModel.CancelRequested += () =>
            {
                // 取消后保持对话框打开，让用户看到取消结果
                // 关闭由 RequestClose 处理
            };
        }
    }
}
