using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<App> _logger;

    public App()
    {
        // 全局异常处理
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "发生未处理的异常");

        System.Windows.MessageBox.Show(
            "程序遇到了一个意外错误，请重试。\n\n如果问题持续出现，请重启应用程序。",
            "RePKG GUI",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogCritical(ex, "发生严重错误");

            System.Windows.MessageBox.Show(
                "程序遇到了严重错误，需要关闭。\n\n请重启应用程序。",
                "RePKG GUI",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 日志配置
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

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
