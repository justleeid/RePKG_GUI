using System;
using System.IO;
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
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public App()
    {
        // 全局异常处理
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();

        // 加载保存的主题
        LoadTheme();
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

        // 检查是否需要显示欢迎页
        var config = LoadConfig();
        if (!config.HasAcceptedDisclaimer)
        {
            var welcomeDialog = new WelcomeDialog();
            if (welcomeDialog.ShowDialog() == true)
            {
                config.HasAcceptedDisclaimer = true;
                SaveConfig(config);
            }
        }

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

    // ── 主题切换 ──

    /// <summary>
    /// 切换深色/浅色主题
    /// </summary>
    public static void ApplyTheme(bool isDark)
    {
        var app = Current;
        if (app == null) return;

        var themeUri = isDark
            ? new Uri("pack://application:,,,/Resources/Themes/Dark.xaml", UriKind.Absolute)
            : new Uri("pack://application:,,,/Resources/Themes/Light.xaml", UriKind.Absolute);

        var themeDict = new ResourceDictionary { Source = themeUri };

        // 替换主题字典（第一个是主题，第二个是样式）
        app.Resources.MergedDictionaries[0] = themeDict;

        // 保存配置
        var config = LoadConfig();
        config.IsDarkMode = isDark;
        SaveConfig(config);
    }

    /// <summary>
    /// 加载保存的主题
    /// </summary>
    private void LoadTheme()
    {
        var config = LoadConfig();
        if (config.IsDarkMode)
        {
            ApplyTheme(true);
        }
    }

    // ── 配置持久化 ──

    private static AppSettings LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // 配置读取失败使用默认值
        }
        return new AppSettings();
    }

    private static void SaveConfig(AppSettings config)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // 配置保存失败不影响主流程
        }
    }
}

/// <summary>
/// 应用配置
/// </summary>
public class AppSettings
{
    public bool HasAcceptedDisclaimer { get; set; }
    public bool IsDarkMode { get; set; }
}
