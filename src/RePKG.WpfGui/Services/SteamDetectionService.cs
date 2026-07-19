using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

    /// <inheritdoc/>
    public string? DetectSteamPath()
    {
        // 1. 尝试从注册表读取
        var registryPath = GetSteamPathFromRegistry();
        if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
            return registryPath;

        // 2. 尝试默认路径
        string[] defaultPaths =
        [
            @"C:\Program Files (x86)\Steam",
            @"D:\Steam",
            @"E:\Steam",
            @"F:\Steam"
        ];

        foreach (var path in defaultPaths)
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
        // 1. 获取主 Steam 路径
        var steamPath = DetectSteamPath();
        if (string.IsNullOrEmpty(steamPath))
            return null;

        // 2. 检查主目录下是否有 Workshop
        var mainWorkshopPath = GetWorkshopPath(steamPath);
        if (Directory.Exists(mainWorkshopPath))
            return mainWorkshopPath;

        // 3. 从 libraryfolders.vdf 读取所有 Steam Library 路径
        var libraryPaths = GetSteamLibraryPaths(steamPath);
        foreach (var libPath in libraryPaths)
        {
            var workshopPath = GetWorkshopPath(libPath);
            if (Directory.Exists(workshopPath))
                return workshopPath;
        }

        return null;
    }

    /// <summary>
    /// 从 libraryfolders.vdf 解析所有 Steam Library 路径
    /// </summary>
    private static List<string> GetSteamLibraryPaths(string steamPath)
    {
        var paths = new List<string>();
        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

        if (!File.Exists(vdfPath))
            return paths;

        try
        {
            var content = File.ReadAllText(vdfPath);

            // 解析 "path"		"F:\\SteamLibrary" 格式
            var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var libPath = match.Groups[1].Value.Replace("\\\\", "\\");
                    if (Directory.Exists(libPath))
                        paths.Add(libPath);
                }
            }
        }
        catch
        {
            // 解析失败不影响主流程
        }

        return paths;
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
