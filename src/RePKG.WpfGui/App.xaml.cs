using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using RePKG.WpfGui.Services;
using RePKG.WpfGui.ViewModels;
using RePKG.WpfGui.Views;

namespace RePKG.WpfGui;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        // 全局异常处理
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show(
            $"发生未处理的异常:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "错误",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"发生严重错误:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "错误",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 服务注册
        services.AddSingleton<ISteamDetectionService, SteamDetectionService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        services.AddTransient<IPkgMetadataService, PkgMetadataService>();
        services.AddTransient<IExtractService, ExtractService>();

        // ViewModel 注册
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MetadataPanelViewModel>();

        // View 注册
        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
