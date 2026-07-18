using System;
using System.Windows;
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
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
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
