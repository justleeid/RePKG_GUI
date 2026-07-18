# RePKG GUI 技术文档

> **文档版本：** V1.0
> **创建日期：** 2026-07-18
> **最后更新：** 2026-07-18
> **关联文档：** [PRD-产品需求文档.md](./PRD-产品需求文档.md) | [UI-设计规范与页面布局.md](./UI-设计规范与页面布局.md)

---

## 目录

1. [系统架构](#1-系统架构)
2. [技术栈](#2-技术栈)
3. [项目结构](#3-项目结构)
4. [核心模块设计](#4-核心模块设计)
5. [数据流设计](#5-数据流设计)
6. [RePKG.Core 集成](#6-repkgcore-集成)
7. [MVVM 架构设计](#7-mvvm-架构设计)
8. [性能优化策略](#8-性能优化策略)
9. [构建与部署](#9-构建与部署)
10. [开发规范](#10-开发规范)
11. [测试策略](#11-测试策略)
12. [附录](#12-附录)

---

## 1. 系统架构

### 1.1 架构总览

采用 **"独立 GUI 项目 + 引用 RePKG.Core 类库"** 的双层架构。GUI 层不直接调用 `repkg.exe` 命令行，而是通过 .NET 项目引用直接调用 RePKG.Core 内部 API，获得更好的性能和错误处理能力。

```
┌─────────────────────────────────────────────────────────┐
│                    RePKG.WpfGui                          │
│  ┌───────────┐  ┌───────────┐  ┌───────────────────┐   │
│  │   Views   │  │ ViewModels│  │     Services      │   │
│  │  (XAML)   │◄─┤  (C#)     │◄─┤  (Business Logic) │   │
│  └───────────┘  └───────────┘  └────────┬──────────┘   │
│                                         │               │
├─────────────────────────────────────────┼───────────────┤
│                    Deps/repkg           │               │
│  ┌──────────────────┐  ┌──────────────┐ │               │
│  │   RePKG.Core     │  │  RePKG.App   │ │               │
│  │  (解析引擎)       │◄─┤  (应用层)     │◄┘               │
│  └──────────────────┘  └──────────────┘                 │
│         │                                               │
│  ┌──────┴──────┐                                        │
│  │  RePKG.Cli  │  (不直接调用，仅参考)                    │
│  └─────────────┘                                        │
└─────────────────────────────────────────────────────────┘
```

### 1.2 架构原则

| 原则 | 说明 | 实现方式 |
|------|------|----------|
| **分层解耦** | GUI 层不依赖 CLI，直接调用 Core API | .NET 项目引用 |
| **MVVM 模式** | 视图与逻辑分离，便于测试和维护 | WPF Data Binding + CommunityToolkit.Mvvm |
| **异步优先** | 所有 I/O 操作异步执行，不阻塞 UI 线程 | `async/await` + `IProgress<T>` |
| **依赖注入** | 服务层通过 DI 容器管理生命周期 | `Microsoft.Extensions.DependencyInjection` |
| **流式处理** | 仅读取 PKG 头部和必要条目，避免全量解密 | RePKG.Core `PkgReader` 流式 API |

---

## 2. 技术栈

### 2.1 核心技术

| 技术 | 版本 | 用途 | 选型理由 |
|------|------|------|----------|
| .NET | 8.0 (LTS) | 运行时与 SDK | 跨平台、高性能、长期支持 |
| C# | 12.0 | 开发语言 | 与 RePKG.Core 同语言，零 FFI 开销 |
| WPF | .NET 8 | UI 框架 | Windows 原生 UI，成熟稳定，硬件加速渲染 |
| XAML | - | 声明式 UI | WPF 原生标记语言，支持热重载 |
| CommunityToolkit.Mvvm | 8.x | MVVM 工具包 | 简化 MVVM 样板代码，源码生成器 |

### 2.2 辅助依赖

| 包名 | 用途 |
|------|------|
| `Microsoft.Extensions.DependencyInjection` | 依赖注入容器 |
| `Microsoft.Extensions.Logging` | 日志抽象 |
| `Serilog.Extensions.Logging` | 结构化日志（文件 + 控制台） |
| `Newtonsoft.Json` / `System.Text.Json` | JSON 序列化（兼容上游依赖） |

### 2.3 为什么不用 WinUI 3 / Avalonia / Electron？

| 方案 | 排除理由 |
|------|----------|
| **WinUI 3** | 生态尚不成熟，第三方控件少；与 RePKG.Core（.NET Standard 2.0）兼容需额外适配 |
| **Avalonia** | 跨平台优势对本项目无意义（目标用户 100% Windows）；WPF 社区资源更丰富 |
| **Electron** | 内存占用高（>200MB 起步）；需 JS/TS 桥接层调用 C# 内核，增加复杂度 |
| **WinForms** | 无法实现现代化 UI（亚克力、流畅动画、虚拟滚动）；数据绑定能力弱 |

**选择 WPF 的核心逻辑：** 目标用户锁定 Windows → 无需跨平台 → WPF 是 Windows 桌面开发的最优解（原生性能 + 丰富生态 + 成熟工具链）。

---

## 3. 项目结构

### 3.1 目录结构

```
RePKG.WpfGui/
├── App.xaml                      # 应用程序入口，全局资源字典
├── App.xaml.cs                   # 启动逻辑，DI 容器初始化
├── appsettings.json              # 用户配置（缓存路径、窗口状态等）
├── RePKG.WpfGui.csproj           # 项目文件，NuGet 引用
│
├── Views/                        # XAML 视图（纯 UI 描述）
│   ├── MainWindow.xaml           # 主窗口：左侧目录树 + 右侧壁纸列表
│   ├── MainWindow.xaml.cs        # Code-behind（最小化，仅 UI 逻辑）
│   ├── MetadataPanel.xaml        # 底部详情面板：标题/作者/标签/大小
│   ├── MetadataPanel.xaml.cs
│   ├── ExtractDialog.xaml        # 解包选项对话框
│   ├── ExtractDialog.xaml.cs
│   ├── ExtractProgressDialog.xaml # 解包进度对话框
│   ├── ExtractProgressDialog.xaml.cs
│   ├── WelcomePage.xaml          # 欢迎页（首次启动）
│   ├── WelcomePage.xaml.cs
│   └── Controls/                 # 自定义可复用控件
│       ├── WallpaperCard.xaml    # 壁纸卡片（网格视图项）
│       ├── WallpaperListItem.xaml # 壁纸列表行（列表视图项）
│       ├── TagChip.xaml          # 标签胶囊
│       └── SkeletonCard.xaml     # 骨架屏占位
│
├── ViewModels/                   # 视图模型（UI 状态 + 命令）
│   ├── MainViewModel.cs          # 主窗口 VM：扫描/筛选/批量操作
│   ├── WallpaperItemViewModel.cs # 列表项 VM：缩略图/选中状态/元数据
│   ├── MetadataPanelViewModel.cs # 详情面板 VM
│   ├── ExtractViewModel.cs       # 解包选项 VM
│   └── ExtractProgressViewModel.cs # 解包进度 VM
│
├── Models/                       # 数据模型（POCO / DTO）
│   ├── WallpaperItem.cs          # 标题/作者/类型/大小/预览图路径
│   ├── ProjectInfo.cs            # 映射 project.json 字段
│   ├── ExtractOptions.cs         # 解包配置参数
│   └── ScanResult.cs             # 扫描结果 DTO
│
├── Services/                     # 业务逻辑层
│   ├── IPkgMetadataService.cs    # 元数据提取接口
│   ├── PkgMetadataService.cs     # 流式读取 PKG 元数据（不全局解密）
│   ├── IPreviewCacheService.cs   # 缩略图缓存接口
│   ├── PreviewCacheService.cs    # 缩略图缓存（内存 LRU + 磁盘文件）
│   ├── IExtractService.cs        # 解包服务接口
│   ├── ExtractService.cs         # 异步解包 + Progress<T> 进度回调
│   ├── ISteamDetectionService.cs # Steam 路径检测接口
│   ├── SteamDetectionService.cs  # 自动检测 Steam 安装路径
│   ├── ISettingsService.cs       # 用户设置持久化接口
│   └── SettingsService.cs        # JSON 文件读写用户配置
│
├── Converters/                   # XAML 值转换器
│   ├── BoolToVisibilityConverter.cs
│   ├── FileSizeConverter.cs      # long → "45.2 MB"
│   ├── TypeToIconConverter.cs    # "scene" → 🎬 图标
│   └── BoolInvertConverter.cs
│
├── Helpers/                      # 工具类
│   ├── AsyncRelayCommand.cs      # 异步命令封装
│   ├── ObservableObject.cs       # 属性变更通知基类
│   └── VirtualizingCollection.cs # 虚拟滚动数据源
│
└── Resources/                    # 静态资源
    ├── Styles/                   # XAML 样式
    │   ├── Colors.xaml           # 色彩定义
    │   ├── Typography.xaml       # 字体定义
    │   ├── Buttons.xaml          # 按钮样式
    │   ├── Cards.xaml            # 卡片样式
    │   └── DarkTheme.xaml        # 暗色主题覆盖
    ├── Icons/                    # 图标资源
    └── Images/                   # 图片资源（Logo、空状态图等）

Deps/
└── repkg/                        # 上游仓库（Git Submodule）
    ├── RePKG.Core/               # 核心解析库（PkgReader、PkgExtractor、TexConverter）
    ├── RePKG.Application/        # 应用层逻辑
    └── RePKG.Cli/                # 命令行工具（本项���不直接引用）
```

### 3.2 项目引用关系

```
RePKG.WpfGui.csproj
  ├── PackageReference: Microsoft.Extensions.DependencyInjection
  ├── PackageReference: CommunityToolkit.Mvvm
  ├── PackageReference: Serilog.Extensions.Logging
  ├── ProjectReference: ..\Deps\repkg\RePKG.Core\RePKG.Core.csproj
  └── ProjectReference: ..\Deps\repkg\RePKG.Application\RePKG.Application.csproj
```

---

## 4. 核心模块设计

### 4.1 元数据提取服务（PkgMetadataService）

**职责：** 对 PKG 文件进行"浅读取"——仅解析头部和 `project.json` / `preview.jpg` 条目，不触发全量解密。

```
┌──────────────────────────────────────────────────────────┐
│                   PkgMetadataService                      │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  输入: string pkgPath (PKG 文件路径)                       │
│                                                          │
│  ┌────────────────────────────────────────────────┐      │
│  │ 1. 读取 PKG Header                              │      │
│  │    ├─ 魔数验证 (PKG 格式标识)                     │      │
│  │    ├─ 版本号                                      │      │
│  │    └─ 条目索引表偏移量                             │      │
│  ├────────────────────────────────────────────────┤      │
│  │ 2. 解析条目索引表                                 │      │
│  │    ├─ 遍历所有 Entry 名称                         │      │
│  │    ├─ 定位 project.json entry                    │      │
│  │    └─ 定位 preview.jpg entry                     │      │
│  ├────────────────────────────────────────────────┤      │
│  │ 3. 流式解压目标条目                               │      │
│  │    ├─ project.json → UTF-8 字符串                │      │
│  │    └─ preview.jpg → byte[]                      │      │
│  ├────────────────────────────────────────────────┤      │
│  │ 4. 反序列化 + 组装                                │      │
│  │    ├─ project.json → ProjectInfo 对象            │      │
│  │    ├─ preview.jpg → BitmapImage (缩略图)         │      │
│  │    └─ 组装 WallpaperItem                         │      │
│  └────────────────────────────────────────────────┘      │
│                                                          │
│  输出: WallpaperItem (标题/作者/类型/缩略图/大小/标签)      │
│                                                          │
│  异常处理:                                                │
│    ├─ 损坏 PKG → 返回 null + 记录警告日志                  │
│    ├─ 加密 PKG → 返回 null + 标记"不支持"                  │
│    └─ 缺失 project.json → 降级为仅显示文件名和大小          │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

**关键代码结构：**

```csharp
public interface IPkgMetadataService
{
    Task<WallpaperItem?> ExtractMetadataAsync(
        string pkgPath,
        CancellationToken cancellationToken = default);
}

public class PkgMetadataService : IPkgMetadataService
{
    private readonly ILogger<PkgMetadataService> _logger;
    
    public async Task<WallpaperItem?> ExtractMetadataAsync(
        string pkgPath,
        CancellationToken cancellationToken = default)
    {
        // 1. 打开文件流
        await using var fileStream = File.OpenRead(pkgPath);
        
        // 2. 读取 PKG Header
        var header = PkgHeader.Read(fileStream);
        
        // 3. 定位 project.json entry
        var entries = header.ReadEntryTable(fileStream);
        var projectEntry = entries.FirstOrDefault(e => 
            e.Name.Equals("project.json", StringComparison.OrdinalIgnoreCase));
        var previewEntry = entries.FirstOrDefault(e =>
            e.Name.Equals("preview.jpg", StringComparison.OrdinalIgnoreCase));
        
        // 4. 流式解压并反序列化
        var projectJson = await DecompressEntryAsync(fileStream, projectEntry, ct);
        var projectInfo = JsonSerializer.Deserialize<ProjectInfo>(projectJson);
        
        var previewBytes = await DecompressEntryAsync(fileStream, previewEntry, ct);
        var previewImage = CreateBitmapFromBytes(previewBytes);
        
        // 5. 组装返回
        return new WallpaperItem
        {
            Title = projectInfo.Title ?? Path.GetFileNameWithoutExtension(pkgPath),
            Author = projectInfo.Author ?? "Unknown",
            Type = projectInfo.Type ?? "unknown",
            PreviewImage = previewImage,
            FileSize = new FileInfo(pkgPath).Length,
            WorkshopId = projectInfo.WorkshopId,
            Tags = projectInfo.Tags ?? Array.Empty<string>(),
            PkgPath = pkgPath,
            ScanTime = DateTime.UtcNow
        };
    }
}
```

### 4.2 缩略图缓存服务（PreviewCacheService）

**职责：** 两级缓存管理，减少 PKG 重复读取和内存占用。

```
┌──────────────────────────────────────────────────────────┐
│                  PreviewCacheService                      │
│                                                          │
│  ┌─────────────────────┐    ┌─────────────────────────┐  │
│  │   L1: 内存缓存       │    │   L2: 磁盘缓存           │  │
│  │   (LRU, 最近500张)   │    │   (%AppData%\Thumbs\)   │  │
│  │                     │    │                         │  │
│  │   命中: ~0.1ms      │    │   命中: ~5ms (SSD)      │  │
│  │   容量: ~100MB      │    │   上限: 500MB           │  │
│  └─────────┬───────────┘    └───────────┬─────────────┘  │
│            │                            │                │
│            └──────────┬─────────────────┘                │
│                       ▼                                  │
│              ┌─────────────────┐                         │
│              │   缓存 Key       │                         │
│              │   SHA256(pkgPath + lastWriteTime)          │
│              └─────────────────┘                         │
│                                                          │
│  写入策略:                                                │
│    ├─ 提取缩略图成功后 → 同时写入 L1 + L2                   │
│    ├─ L2 超出上限 → 按 LRU 清理最旧文件                     │
│    └─ L1 超出容量 → 淘汰最久未访问项                        │
│                                                          │
│  失效策略:                                                │
│    ├─ PKG 文件修改时间变化 → 缓存失效                       │
│    └─ 用户手动"清除缓存" → 清空 L1 + L2                    │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

**关键代码结构：**

```csharp
public interface IPreviewCacheService
{
    Task<BitmapImage?> GetAsync(string pkgPath, DateTime lastWriteTime);
    Task SetAsync(string pkgPath, DateTime lastWriteTime, BitmapImage preview);
    Task InvalidateAsync(string pkgPath);
    Task ClearAsync();
    CacheStats GetStats();
}

public class PreviewCacheService : IPreviewCacheService, IDisposable
{
    private readonly MemoryCache _memoryCache;        // L1: Microsoft.Extensions.Caching.Memory
    private readonly string _diskCacheDir;             // L2: 文件系统
    private readonly long _maxDiskCacheBytes;
    private readonly int _maxMemoryItems;
    
    private string ComputeCacheKey(string pkgPath, DateTime lastWriteTime)
    {
        var raw = $"{pkgPath}|{lastWriteTime.Ticks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
    
    // ... 两级缓存读/写/淘汰实现
}

public record CacheStats
{
    public int MemoryItemCount { get; init; }
    public long MemorySizeBytes { get; init; }
    public int DiskFileCount { get; init; }
    public long DiskSizeBytes { get; init; }
    public double MemoryHitRate { get; init; }
    public double DiskHitRate { get; init; }
}
```

### 4.3 解包服务（ExtractService）

**职责：** 异步批量解包，细粒度进度回调，错误隔离。

```
┌──────────────────────────────────────────────────────────┐
│                     ExtractService                        │
│                                                          │
│  输入:                                                    │
│    ├─ WallpaperItem[] items (待解包列表)                    │
│    ├─ ExtractOptions options (解包配置)                     │
│    └─ IProgress<ExtractProgress> progress (进度回调)        │
│                                                          │
│  执行流程:                                                │
│  ┌────────────────────────────────────────────────┐      │
│  │ for each item in items (sequential):           │      │
│  │   1. report: "开始解包 {item.Title}"             │      │
│  │   2. 计算输出路径                                 │      │
│  │      if UseNameAsFolder:                        │      │
│  │        outDir = baseDir / sanitize(item.Title)  │      │
│  │      else:                                      │      │
│  │        outDir = baseDir / item.WorkshopId       │      │
│  │   3. PkgExtractor.Extract(                       │      │
│  │        pkgPath, outDir,                         │      │
│  │        includeExtensions,                        │      │
│  │        excludeExtensions,                        │      │
│  │        convertTex: options.ConvertTexToImage,   │      │
│  │        overwrite: options.OverwriteExisting)    │      │
│  │   4. if success:                                │      │
│  │        item.IsExtracted = true                  │      │
│  │        report: "完成 {item.Title}"                │      │
│  │      if error:                                  │      │
│  │        report: "失败 {item.Title}: {message}"      │      │
│  │        continue to next item                     │      │
│  │   5. 支持 CancellationToken 取消                  │      │
│  └────────────────────────────────────────────────┘      │
│                                                          │
│  输出: ExtractResult (成功数/失败数/错误列表)               │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

**进度回调模型：**

```csharp
public record ExtractProgress
{
    public int TotalItems { get; init; }          // 总项目数
    public int CompletedItems { get; init; }      // 已完成数
    public int FailedItems { get; init; }         // 失败数
    public string? CurrentItemTitle { get; init; } // 当前处理项
    public long CurrentItemBytesProcessed { get; init; } // 当前项已处理字节
    public long CurrentItemTotalBytes { get; init; }     // 当前项总字节
    public ExtractItemStatus Status { get; init; }       // 状态
    public string? ErrorMessage { get; init; }           // 错误信息
}

public enum ExtractItemStatus
{
    Pending,        // 等待中
    Extracting,     // 解包中
    Converting,     // TEX 转换中
    Completed,      // 完成
    Failed,         // 失败
    Skipped         // 跳过（已存在）
}

public record ExtractResult
{
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public int SkippedCount { get; init; }
    public List<ExtractError> Errors { get; init; } = new();
    public TimeSpan Elapsed { get; init; }
}

public record ExtractError
{
    public string PkgPath { get; init; } = "";
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
}
```

### 4.4 Steam 路径检测服务（SteamDetectionService）

**职责：** 自动检测 Steam 安装路径和 Workshop 目录。

```
检测策略（按优先级）:
  1. 注册表: HKLM\SOFTWARE\WOW6432Node\Valve\Steam\InstallPath
  2. 默认路径:
     ├─ C:\Program Files (x86)\Steam
     ├─ D:\Steam
     └─ E:\Steam
  3. 正在运行的 Steam 进程路径
  4. 用户手动选择的路径

检测到后自动拼接:
  {SteamPath}\steamapps\workshop\content\431960
```

---

## 5. 数据流设计

### 5.1 主流程数据流

```
┌──────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────┐
│  User    │    │  MainVM      │    │  Services    │    │  RePKG   │
│  (UI)    │    │  (ViewModel) │    │  (Business)  │    │  .Core   │
└────┬─────┘    └──────┬───────┘    └──────┬───────┘    └────┬─────┘
     │                 │                   │                 │
     │ 点击"扫描"       │                   │                 │
     │────────────────►│                   │                 │
     │                 │                   │                 │
     │                 │ DiscoverPkgs()    │                 │
     │                 │──────────────────►│                 │
     │                 │                   │ Directory.EnumerateFiles("*.pkg")
     │                 │                   │────────────────►│
     │                 │                   │                 │
     │                 │                   │  foreach pkg:   │
     │                 │                   │  PkgReader.ReadHeader()
     │                 │                   │────────────────►│
     │                 │                   │ ◄────────────────│
     │                 │                   │  ReadEntry("project.json")
     │                 │                   │────────────────►│
     │                 │                   │ ◄────────────────│
     │                 │                   │  ReadEntry("preview.jpg")
     │                 │                   │────────────────►│
     │                 │                   │ ◄────────────────│
     │                 │                   │                 │
     │                 │ report progress   │                 │
     │                 │◄──────────────────│                 │
     │                 │                   │                 │
     │ UI 刷新列表      │                   │                 │
     │◄────────────────│                   │                 │
     │                 │                   │                 │
```

### 5.2 解包流程数据流

```
┌──────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────┐
│  User    │    │  ExtractVM   │    │  ExtractSvc  │    │  RePKG   │
│  (UI)    │    │  (ViewModel) │    │  (Service)   │    │  .Core   │
└────┬─────┘    └──────┬───────┘    └──────┬───────┘    └────┬─────┘
     │                 │                   │                 │
     │ 点击"解包"       │                   │                 │
     │────────────────►│                   │                 │
     │                 │ ExtractAsync()    │                 │
     │                 │──────────────────►│                 │
     │                 │                   │                 │
     │                 │ Progress<T>       │ PkgExtractor   │
     │                 │◄──────────────────│ .Extract()     │
     │                 │                   │───────────────►│
     │                 │                   │                 │
     │ UI 进度条        │                   │  Entry callback │
     │◄────────────────│                   │◄────────────────│
     │                 │                   │                 │
     │                 │                   │  if .tex file:  │
     │                 │                   │  TexConverter   │
     │                 │                   │────────────────►│
     │                 │                   │                 │
     │                 │   完成通知         │                 │
     │◄────────────────│◄──────────────────│                 │
     │                 │                   │                 │
```

### 5.3 缓存读取流程

```
请求缩略图
    │
    ▼
┌─────────────┐
│ L1 内存缓存  │──命中──► 返回 BitmapImage (~0.1ms)
└──────┬──────┘
       │ 未命中
       ▼
┌─────────────┐
│ L2 磁盘缓存  │──命中──► 读取 + 写入 L1 + 返回 (~5ms SSD)
└──────┬──────┘
       │ 未命中
       ▼
┌─────────────┐
│ PKG 流式读取 │─────────► 解压 preview.jpg
│ (仅目标条目) │           ├─ 写入 L1 (内存)
└─────────────┘           ├─ 写入 L2 (磁盘)
                          └─ 返回 BitmapImage (~50-200ms)
```

---

## 6. RePKG.Core 集成

### 6.1 上游仓库接入方式

采用 **Git Submodule** 方式引用上游 [notscuffed/repkg](https://github.com/notscuffed/repkg)：

```bash
# 添加 Submodule
git submodule add https://github.com/notscuffed/repkg.git Deps/repkg

# 锁定版本（如 v2.0.0 或特定 commit）
cd Deps/repkg
git checkout <version-tag-or-commit-hash>
cd ../..
git add Deps/repkg
git commit -m "Pin RePKG to <version>"
```

### 6.2 核心 API 使用

#### PkgReader —— 流式读取 PKG

```csharp
// 来源: RePKG.Core.PkgReader
// 用途: 读取 PKG 头部和条目表，不解压全部内容

using var stream = File.OpenRead(pkgPath);
var reader = new PkgReader(stream);

// 读取头部（不含内容数据）
var header = reader.ReadHeader();

// 获取所有条目名称列表（仅索引，不读取内容）
var entryNames = header.Entries.Select(e => e.Name).ToList();

// 定位目标条目
var projectEntry = header.Entries.FirstOrDefault(e => 
    e.Name.EndsWith("project.json", StringComparison.OrdinalIgnoreCase));

// 仅解压目标条目
if (projectEntry != null)
{
    var jsonBytes = reader.ReadEntryContent(projectEntry);
    var json = Encoding.UTF8.GetString(jsonBytes);
}
```

#### PkgExtractor —— 解包 PKG

```csharp
// 来源: RePKG.Core.PkgExtractor
// 用途: 将 PKG 内容解压到指定目录

var extractor = new PkgExtractor();
var options = new ExtractOptions
{
    OutputDirectory = @"D:\output\12345678",
    IncludeExtensions = new[] { ".png", ".jpg" },
    ExcludeExtensions = new[] { ".wav" },
    ConvertTexToImage = true,
    OverwriteExisting = false,
    CopyProjectFiles = true
};

// 同步解包（在后台线程调用）
extractor.Extract(pkgPath, options);

// 或使用带进度的异步封装
await Task.Run(() => 
{
    extractor.Extract(pkgPath, options, 
        onEntryExtracted: (entryName, bytesProcessed, totalBytes) =>
        {
            // 进度回调
        });
});
```

#### TexConverter —— TEX 转 PNG

```csharp
// 来源: RePKG.Core.TexConverter
// 用途: 将 Wallpaper Engine .tex 纹理转换为 PNG 图片

var converter = new TexConverter();
var texBytes = File.ReadAllBytes(@"path\to\texture.tex");
var pngBytes = converter.ConvertToPng(texBytes);
File.WriteAllBytes(@"path\to\output.png", pngBytes);
```

### 6.3 上游 API 变更适配策略

```
GUI Services Layer
    │
    ├── IPkgMetadataService  ←── 依赖接口，不依赖具体类
    ├── IExtractService      ←── 依赖接口
    └── ITexConvertService   ←── 依赖接口
            │
            ▼
    Adapter Layer (适配层)
            │
            ▼
    RePKG.Core (上游，可能变更)
```

当上游 API 变更时：
1. **更新 Submodule** → `git submodule update --remote`
2. **修改适配层** → 仅修改 `Services/` 下的实现类
3. **GUI 层零修改** → ViewModel / View 层通过接口调用，不受影响
4. **编译验证** → `dotnet build`
5. **回归测试** → 运行测试套件

---

## 7. MVVM 架构设计

### 7.1 MVVM 模式概览

```
┌──────────────────────────────────────────────────────────┐
│                        View (XAML)                        │
│  ┌────────────────────────────────────────────────────┐  │
│  │ 纯 UI 描述 + 数据绑定表达式                           │  │
│  │ 仅处理 UI 特定逻辑（动画、拖拽、焦点管理）               │  │
│  └───────────────────────┬────────────────────────────┘  │
│                          │ Data Binding                   │
│                          │ Commands                       │
│  ┌───────────────────────▼────────────────────────────┐  │
│  │                   ViewModel (C#)                     │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │ UI 状态 (ObservableProperty)                   │  │  │
│  │  │ 命令 (RelayCommand / AsyncRelayCommand)        │  │  │
│  │  │ 调用 Service 层                                │  │  │
│  │  │ 将 Model 转换为 UI 友好的展示数据                │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  └───────────────────────┬────────────────────────────┘  │
│                          │                                │
│  ┌───────────────────────▼────────────────────────────┐  │
│  │                     Model (C#)                      │  │
│  │  纯数据对象 (POCO) + 业务实体                         │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

### 7.2 关键 ViewModel 设计

#### MainViewModel

```csharp
public partial class MainViewModel : ObservableObject
{
    // ── 依赖注入 ──
    private readonly IPkgMetadataService _metadataService;
    private readonly IPreviewCacheService _cacheService;
    private readonly IExtractService _extractService;
    private readonly ISteamDetectionService _steamService;
    
    // ── 可观察属性 ──
    [ObservableProperty]
    private ObservableCollection<WallpaperItemViewModel> _wallpaperItems = new();
    
    [ObservableProperty]
    private WallpaperItemViewModel? _selectedItem;
    
    [ObservableProperty]
    private bool _isScanning;
    
    [ObservableProperty]
    private int _totalCount;
    
    [ObservableProperty]
    private int _selectedCount;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Grid;
    
    [ObservableProperty]
    private SortField _sortField = SortField.Title;
    
    [ObservableProperty]
    private bool _sortAscending = true;
    
    // ── 命令 ──
    [RelayCommand]
    private async Task OpenDirectoryAsync() { /* ... */ }
    
    [RelayCommand]
    private async Task ScanDirectoryAsync(string path) { /* ... */ }
    
    [RelayCommand]
    private void ToggleViewMode() { /* ... */ }
    
    [RelayCommand]
    private async Task ExtractSelectedAsync() { /* ... */ }
    
    [RelayCommand]
    private void SelectAll() { /* ... */ }
    
    [RelayCommand]
    private void DeselectAll() { /* ... */ }
    
    // ── 筛选逻辑 ──
    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();
        
        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i => 
                i.Title.ToLowerInvariant().Contains(search) ||
                i.Author.ToLowerInvariant().Contains(search) ||
                i.Tags.Any(t => t.ToLowerInvariant().Contains(search)));
        }
        
        // 排序
        filtered = SortField switch
        {
            SortField.Title => SortAscending 
                ? filtered.OrderBy(i => i.Title) 
                : filtered.OrderByDescending(i => i.Title),
            SortField.Author => SortAscending 
                ? filtered.OrderBy(i => i.Author) 
                : filtered.OrderByDescending(i => i.Author),
            SortField.FileSize => SortAscending 
                ? filtered.OrderBy(i => i.FileSize) 
                : filtered.OrderByDescending(i => i.FileSize),
            SortField.Type => SortAscending 
                ? filtered.OrderBy(i => i.Type) 
                : filtered.OrderByDescending(i => i.Type),
            _ => filtered
        };
        
        WallpaperItems = new ObservableCollection<WallpaperItemViewModel>(filtered);
    }
}
```

#### WallpaperItemViewModel

```csharp
public partial class WallpaperItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _author = string.Empty;
    
    [ObservableProperty]
    private string _type = string.Empty;
    
    [ObservableProperty]
    private BitmapImage? _previewImage;
    
    [ObservableProperty]
    private long _fileSize;
    
    [ObservableProperty]
    private string _fileSizeDisplay = string.Empty; // "45.2 MB"
    
    [ObservableProperty]
    private string[] _tags = Array.Empty<string>();
    
    [ObservableProperty]
    private string _workshopId = string.Empty;
    
    [ObservableProperty]
    private string _pkgPath = string.Empty;
    
    [ObservableProperty]
    private bool _isSelected;
    
    [ObservableProperty]
    private bool _isExtracted;
    
    [ObservableProperty]
    private string _typeIcon = string.Empty; // 🎬 / 🌍 / 📹 / 📱
    
    // 状态背景颜色（已解包/未解包/扫描失败）
    [ObservableProperty]
    private ItemStatus _status = ItemStatus.Ready;
    
    partial void OnIsSelectedChanged(bool value)
    {
        // 通知父 VM 更新选中计数
        SelectionChanged?.Invoke(this, value);
    }
    
    public event Action<WallpaperItemViewModel, bool>? SelectionChanged;
}

public enum ItemStatus
{
    Scanning,      // 正在扫描元数据
    Ready,          // 就绪
    Extracted,      // 已解包
    ScanFailed      // 扫描失败
}
```

### 7.3 数据绑定示例

**MainWindow.xaml 关键绑定：**

```xml
<Window x:Class="RePKG.WpfGui.Views.MainWindow"
        xmlns:vm="clr-namespace:RePKG.WpfGui.ViewModels"
        xmlns:conv="clr-namespace:RePKG.WpfGui.Converters">
    
    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>
    
    <!-- 工具栏命令绑定 -->
    <Button Content="📁 打开目录" 
            Command="{Binding OpenDirectoryCommand}"/>
    
    <!-- 搜索框双向绑定 -->
    <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
             PlaceholderText="🔍 搜索壁纸..."/>
    
    <!-- 壁纸列表 ItemsControl 绑定到集合 -->
    <ItemsControl ItemsSource="{Binding WallpaperItems}">
        <ItemsControl.ItemTemplate>
            <DataTemplate DataType="{x:Type vm:WallpaperItemViewModel}">
                <local:WallpaperCard/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
    
    <!-- 状态栏信息绑定 -->
    <TextBlock Text="{Binding TotalCount, StringFormat='共 {0} 个壁纸'}"/>
    <TextBlock Text="{Binding SelectedCount, StringFormat='已选 {0} 个'}"/>
</Window>
```

---

## 8. 性能优化策略

### 8.1 扫描阶段

| 策略 | 说明 | 实现 |
|------|------|------|
| **流式读取** | 仅解压 project.json + preview.jpg | PkgReader.ReadEntryContent() 按需读取 |
| **异步后台** | 扫描不阻塞 UI 线程 | `Task.Run` + `IProgress<T>` 回调 |
| **并发控制** | 限制同时解压的 PKG 数量 | `SemaphoreSlim(Environment.ProcessorCount)` |
| **延迟加载** | 缩略图仅渲染可视区域 | `VirtualizingStackPanel` + `IAsyncVirtualizingCollection` |
| **取消支持** | 扫描过程可随时取消 | `CancellationToken` 传递到每个操作 |

### 8.2 浏览阶段

| 策略 | 说明 | 实现 |
|------|------|------|
| **虚拟滚动** | 仅渲染可视区域的列表项 | WPF `VirtualizingStackPanel.IsVirtualizing="True"` |
| **缩略图懒加载** | 滚动到可视区时才加载缩略图 | `PriorityBinding` + 占位符图片 |
| **缩略图缓存** | 两级缓存减少重复 I/O | L1 内存 LRU + L2 磁盘文件 |
| **UI 虚拟化** | 不创建不可见项的 ViewModel | `IAsyncVirtualizingCollection` 按需创建 |
| **图片异步解码** | 缩略图在后台线程解码 | `BitmapImage.DecodePixelWidth` + 后台线程 |

### 8.3 解包阶段

| 策略 | 说明 | 实现 |
|------|------|------|
| **串行解包** | 避免磁盘 I/O 竞争 | 逐个 PKG 串行执行 |
| **大文件缓冲** | 控制内存峰值 | 流式复制，不一次性加载到内存 |
| **TEX 转换批处理** | 收集 TEX 后批量转换 | 减少 TexConverter 初始化次数 |

### 8.4 内存管理

```csharp
// 缩略图内存管控
public class ThumbnailMemoryManager
{
    private const int MaxPixelWidth = 200;     // 网格缩略图最大宽度
    private const int MaxCacheItems = 500;     // 内存缓存上限
    private const long MaxMemoryBytes = 100 * 1024 * 1024; // 100MB
    
    public BitmapImage DecodeThumbnail(byte[] imageBytes)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.DecodePixelWidth = MaxPixelWidth; // 解码时缩放到目标尺寸
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = new MemoryStream(imageBytes);
        bitmap.EndInit();
        bitmap.Freeze(); // 冻结后可跨线程使用
        return bitmap;
    }
}
```

---

## 9. 构建与部署

### 9.1 构建配置

#### 项目文件（.csproj）

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.17763.0</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>Resources\app.ico</ApplicationIcon>
    <AssemblyName>RePKG.Gui</AssemblyName>
    <RootNamespace>RePKG.WpfGui</RootNamespace>
    <Version>1.0.0</Version>
    <Authors>RePKG GUI Contributors</Authors>
    <Description>Wallpaper Engine PKG/TEX 解包工具图形界面</Description>
  </PropertyGroup>

  <!-- 发布配置 -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishReadyToRun>true</PublishReadyToRun>
    <PublishTrimmed>true</PublishTrimmed>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>

</Project>
```

### 9.2 发布命令

```bash
# 开发构建
dotnet build -c Debug

# 发布构建（自包含单文件）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/

# 生成安装包（使用 WiX Toolset 或 Inno Setup）
# 见 ./installer/ 目录下的安装脚本
```

### 9.3 发布物

| 文件 | 说明 |
|------|------|
| `RePKG.Gui.exe` | 自包含单文件可执行程序 |
| `RePKG.Gui.Setup.msi` | Windows 安装包（可选） |
| `RePKG.Gui.Portable.zip` | 便携版压缩包（可选） |

### 9.4 安装目录结构

```
%LocalAppData%\RePKG.Gui\        (安装目录)
├── RePKG.Gui.exe                (主程序)
├── RePKG.Gui.dll                (主程序依赖)
├── RePKG.Core.dll               (打包后的 Core 库)
├── *.dll                        (其他运行时依赖)
└── appsettings.json             (默认配置)

%AppData%\RePKG.Gui\             (用户数据目录)
├── settings.json                (用户设置)
├── Thumbs\                      (缩略图磁盘缓存)
│   ├── A1B2C3.jpg
│   ├── D4E5F6.jpg
│   └── ...
└── logs\                        (应用日志)
    ├── repkg-gui-20260718.log
    └── ...
```

---

## 10. 开发规范

### 10.1 命名规范

| 类型 | 规范 | 示例 |
|------|------|------|
| 命名空间 | PascalCase，按文件夹层级 | `RePKG.WpfGui.Services` |
| 类 / 接口 | PascalCase，接口加 `I` 前缀 | `PkgMetadataService` / `IPkgMetadataService` |
| 方法 | PascalCase，动词开头（async 方法以 Async 结尾） | `ExtractMetadataAsync()` |
| 属性 | PascalCase | `WallpaperItems` |
| 私有字段 | _camelCase | `_metadataService` |
| 局部变量 | camelCase | `pkgPath` |
| XAML 资源 Key | PascalCase | `PrimaryButtonStyle` |
| 常量 | PascalCase | `MaxCacheItems` |

### 10.2 代码组织

```csharp
// 每个文件一个类
// 类结构顺序：
public class ExampleService : IExampleService
{
    // 1. 私有字段
    private readonly ILogger<ExampleService> _logger;
    private readonly IDependencyService _dependency;
    
    // 2. 构造函数
    public ExampleService(ILogger<ExampleService> logger, IDependencyService dependency)
    {
        _logger = logger;
        _dependency = dependency;
    }
    
    // 3. 公共属性
    public int OperationCount { get; private set; }
    
    // 4. 公共方法
    public async Task<Result> DoSomethingAsync(CancellationToken ct = default)
    {
        // ...
    }
    
    // 5. 私有方法
    private void HelperMethod()
    {
        // ...
    }
}
```

### 10.3 错误处理

```csharp
// 服务层：捕获异常，返回 Result 类型或 null，不抛出
public async Task<WallpaperItem?> ExtractMetadataAsync(string pkgPath, CancellationToken ct)
{
    try
    {
        // ... 正常逻辑
    }
    catch (InvalidDataException ex)
    {
        _logger.LogWarning(ex, "PKG 文件损坏: {PkgPath}", pkgPath);
        return null; // 损坏文件返回 null，不影响其他文件
    }
    catch (OperationCanceledException)
    {
        throw; // 取消操作向上传播
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "未知错误: {PkgPath}", pkgPath);
        return null;
    }
}

// ViewModel 层：处理 null，向用户展示友好信息
```

### 10.4 日志规范

```csharp
// 使用 Serilog 结构化日志
_logger.LogInformation("开始扫描目录: {Directory}", path);
_logger.LogDebug("PKG 条目数: {Count}, 文件: {Path}", entries.Count, pkgPath);
_logger.LogWarning("跳过损坏的 PKG: {Path}, 原因: {Reason}", pkgPath, reason);
_logger.LogError(ex, "解包失败: {Path}", pkgPath);

// 日志级别:
// Trace   - 详细调试信息（如每个字节偏移量）
// Debug   - 开发调试信息（如条目名称列表）
// Information - 关键操作节点（扫描开始/完成、解包开始/完成）
// Warning - 可恢复的异常（损坏文件跳过）
// Error   - 不可恢复的错误（整个操作失败）
```

### 10.5 Git 规范

| 分支 | 用途 |
|------|------|
| `main` | 稳定版本，仅通过 PR 合并 |
| `develop` | 开发主线 |
| `feature/<name>` | 功能开发分支 |
| `fix/<name>` | 问题修复分支 |
| `release/<version>` | 发布准备分支 |

**Commit Message 格式：**
```
<type>(<scope>): <subject>

[body]

[footer]
```

类型：`feat` / `fix` / `refactor` / `perf` / `docs` / `style` / `test` / `chore`

---

## 11. 测试策略

### 11.1 测试金字塔

```
         ┌──────┐
         │ E2E  │  手工验收测试 + 关键流程自动化
         │ 5%   │
        ┌┴──────┴┐
        │ 集成测试 │  Service 层 × RePKG.Core 真实调用
        │  15%   │
       ┌┴────────┴┐
       │  单元测试  │  ViewModel 逻辑 / Converter / Model 验证
       │   80%    │
      └───────────┘
```

### 11.2 测试框架

| 类型 | 框架 | 覆盖范围 |
|------|------|----------|
| 单元测试 | xUnit.net + Moq | ViewModel、Converter、Helper |
| 集成测试 | xUnit.net | Service 层 + 真实的测试 PKG 文件 |
| UI 测试 | WinAppDriver / FlaUI | 关键用户流程（可选） |

### 11.3 测试用例示例

```csharp
// PkgMetadataService 集成测试
public class PkgMetadataServiceTests
{
    private readonly IPkgMetadataService _service;
    private readonly ITestOutputHelper _output;
    
    [Fact]
    public async Task ExtractMetadata_ValidPkg_ReturnsWallpaperItem()
    {
        // Arrange
        var testPkgPath = Path.Combine("TestData", "valid_wallpaper.pkg");
        
        // Act
        var result = await _service.ExtractMetadataAsync(testPkgPath);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
        Assert.NotEmpty(result.Author);
        Assert.NotEmpty(result.Type);
        Assert.True(result.FileSize > 0);
    }
    
    [Fact]
    public async Task ExtractMetadata_CorruptedPkg_ReturnsNull()
    {
        // Arrange
        var testPkgPath = Path.Combine("TestData", "corrupted.pkg");
        
        // Act
        var result = await _service.ExtractMetadataAsync(testPkgPath);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task ExtractMetadata_MissingProjectJson_ReturnsDegradedItem()
    {
        // Arrange
        var testPkgPath = Path.Combine("TestData", "no_project_json.pkg");
        
        // Act
        var result = await _service.ExtractMetadataAsync(testPkgPath);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFileNameWithoutExtension(testPkgPath), result.Title);
        Assert.Equal("Unknown", result.Author);
    }
}

// MainViewModel 单元测试
public class MainViewModelTests
{
    private readonly Mock<IPkgMetadataService> _metadataMock;
    private readonly MainViewModel _vm;
    
    [Fact]
    public void SearchText_FiltersWallpaperItems()
    {
        // Arrange
        _vm.WallpaperItems = new ObservableCollection<WallpaperItemViewModel>
        {
            new() { Title = "Mountain Lake", Author = "Alice" },
            new() { Title = "City Night", Author = "Bob" },
            new() { Title = "Ocean View", Author = "Alice" },
        };
        
        // Act
        _vm.SearchText = "alice";
        
        // Assert - 应筛选出 Author 包含 "alice" 的项
        Assert.Equal(2, _vm.WallpaperItems.Count);
    }
    
    [Fact]
    public void SelectAll_SetsAllItemsSelected()
    {
        // Arrange
        _vm.WallpaperItems = new ObservableCollection<WallpaperItemViewModel>
        {
            new() { IsSelected = false },
            new() { IsSelected = false },
            new() { IsSelected = true },
        };
        
        // Act
        _vm.SelectAllCommand.Execute(null);
        
        // Assert
        Assert.All(_vm.WallpaperItems, item => Assert.True(item.IsSelected));
        Assert.Equal(3, _vm.SelectedCount);
    }
}
```

### 11.4 测试数据

```
TestData/
├── valid_wallpaper.pkg      # 标准格式 PKG（含完整 project.json + preview.jpg）
├── no_preview.pkg           # 缺少 preview.jpg 的 PKG
├── no_project_json.pkg      # 缺少 project.json 的 PKG
├── corrupted.pkg            # 损坏的 PKG（截断文件）
├── encrypted.pkg            # 加密的 PKG（如有新版格式）
├── large_wallpaper.pkg      # 大型 PKG（>500MB，含多个 TEX）
├── empty.pkg                # 空 PKG
├── various_types/           # 各类型壁纸
│   ├── scene_wallpaper.pkg
│   ├── web_wallpaper.pkg
│   └── video_wallpaper.pkg
└── batch/                   # 批量测试用（100 个小 PKG）
    ├── wallpaper_001.pkg
    ├── wallpaper_002.pkg
    └── ...
```

---

## 12. 附录

### 12.1 依赖注入配置

```csharp
// App.xaml.cs
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    
    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // 日志
        services.AddLogging(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(AppDataDir, "logs", "repkg-gui-.log"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger());
        });
        
        // 服务注册（单例，应用生命周期）
        services.AddSingleton<ISteamDetectionService, SteamDetectionService>();
        services.AddSingleton<IPreviewCacheService, PreviewCacheService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        
        // 服务注册（瞬时，每次请求创建新实例）
        services.AddTransient<IPkgMetadataService, PkgMetadataService>();
        services.AddTransient<IExtractService, ExtractService>();
        services.AddTransient<ITexConvertService, TexConvertService>();
        
        // ViewModel 注册
        services.AddTransient<MainViewModel>();
        services.AddTransient<MetadataPanelViewModel>();
        services.AddTransient<ExtractViewModel>();
        services.AddTransient<ExtractProgressViewModel>();
        
        // View 注册
        services.AddTransient<MainWindow>();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }
}
```

### 12.2 关键配置文件

**appsettings.json：**
```json
{
  "App": {
    "Name": "RePKG GUI",
    "Version": "1.0.0"
  },
  "Cache": {
    "MemoryMaxItems": 500,
    "DiskMaxBytes": 524288000,
    "DiskPath": "%AppData%\\RePKG.Gui\\Thumbs"
  },
  "Scan": {
    "MaxConcurrency": 4,
    "DefaultWorkshopPath": "steamapps\\workshop\\content\\431960"
  },
  "Extract": {
    "DefaultOutputDir": ".\\output",
    "UseNameAsFolder": false,
    "ConvertTexToImage": true,
    "OverwriteExisting": false
  },
  "UI": {
    "DefaultViewMode": "Grid",
    "Theme": "System",
    "Language": "zh-CN",
    "DetailPanelDefaultOpen": false
  }
}
```

### 12.3 技术术语表

| 术语 | 说明 |
|------|------|
| **PKG** | Wallpaper Engine 的私有资源封装格式 |
| **TEX** | Wallpaper Engine 的自定义纹理格式 |
| **project.json** | 每个 PKG 内的元数据文件，包含标题、作者、类型等信息 |
| **WPF** | Windows Presentation Foundation，.NET 桌面 UI 框架 |
| **MVVM** | Model-View-ViewModel，UI 架构模式 |
| **DI** | Dependency Injection，依赖注入 |
| **LRU** | Least Recently Used，最近最少使用缓存淘汰算法 |
| **虚拟滚动** | 仅渲染可视区域列表项的 UI 性能优化技术 |
| **亚克力材质** | Acrylic，Windows Fluent Design 的半透明模糊效果 |
| **Git Submodule** | Git 的子仓库管理机制 |
| **自包含发布** | Self-contained publish，将 .NET Runtime 打包进 exe，用户无需额外安装 |

### 12.4 参考资源

| 资源 | 链接 |
|------|------|
| RePKG 上游仓库 | https://github.com/notscuffed/repkg |
| .NET 8 文档 | https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8 |
| WPF 文档 | https://learn.microsoft.com/dotnet/desktop/wpf |
| CommunityToolkit.Mvvm | https://github.com/CommunityToolkit/dotnet |
| Fluent Design 指南 | https://fluent2.microsoft.design |
| xUnit.net | https://xunit.net |

---

**文档结束**
