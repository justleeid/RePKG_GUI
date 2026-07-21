using System.Windows;

namespace RePKG.WpfGui.Views;

/// <summary>
/// WelcomeDialog.xaml 的交互逻辑
/// </summary>
public partial class WelcomeDialog : Window
{
    public WelcomeDialog()
    {
        InitializeComponent();
    }

    private void OnAcceptClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
