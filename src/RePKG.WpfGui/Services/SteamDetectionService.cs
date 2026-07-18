using System;
using System.IO;
using Microsoft.Win32;

namespace RePKG.WpfGui.Services;

/// <summary>
/// Steam 路径检测服务实现
/// </summary>
public class SteamDetectionService : ISteamDetectionService
{
    private const string WallpaperEngineWorkshopId = "431960";
    private const string SteamRegistryKey = @"SOFTWARE\WOW6432Node\Valve\Steam";
    private const string SteamRegistryValue = "InstallPath";

    private static readonly string[] DefaultSteamPaths =
    [
        @"C:\Program Files (x86)\Steam",
        @"D:\Steam",
        @"E:\Steam",
        @"F:\Steam"
    ];

    /// <inheritdoc/>
    public string? DetectSteamPath()
    {
        // 1. 尝试从注册表读取
        var registryPath = GetSteamPathFromRegistry();
        if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
            return registryPath;

        // 2. 尝试默认路径
        foreach (var path in DefaultSteamPaths)
        {
            if (Directory.Exists(path))
                return path;
        }

        return null;
    }

    /// <inheritdoc/>
    public string GetWorkshopPath(string steamPath)
    {
        return Path.Combine(steamPath, "steamapps", "workshop", "content", WallpaperEngineWorkshopId);
    }

    /// <inheritdoc/>
    public string? DetectWorkshopDirectory()
    {
        var steamPath = DetectSteamPath();
        if (string.IsNullOrEmpty(steamPath))
            return null;

        var workshopPath = GetWorkshopPath(steamPath);
        return Directory.Exists(workshopPath) ? workshopPath : null;
    }

    private static string? GetSteamPathFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(SteamRegistryKey);
            return key?.GetValue(SteamRegistryValue) as string;
        }
        catch
        {
            return null;
        }
    }
}
