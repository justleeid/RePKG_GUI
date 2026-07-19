# RePKG GUI

> Wallpaper Engine PKG/TEX 解包工具 — 图形界面版

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-lightgrey.svg)]()

**RePKG GUI** 是一款面向 Wallpaper Engine 普通用户的 Windows 桌面工具。核心价值是：**在不解包全部文件的前提下，以可视化列表（缩略图 + 元数据）方式浏览 PKG 壁纸内容，并支持按需选择性解包。**

---

## ✨ 功能特性

### 浏览与预览
- 🔍 **可视化浏览** — 网格/列表视图展示壁纸缩略图、标题、作者、类型
- 📊 **元数据预览** — 不解包即可查看 project.json 中的标题、标签、大小等信息
- 🖼️ **缩略图缓存** — 两级缓存（内存 LRU 500张 + 磁盘 500MB），快速加载
- 📂 **拖拽导入** — 直接拖拽文件夹到窗口即可扫描

### 解包功能
- ✂️ **选择性解包** — 勾选想要的壁纸，一键解包到指定目录
- 🖼️ **TEX 转换** — 自动将 .tex 纹理文件转换为 PNG/GIF/MP4
- 📁 **灵活输出** — 支持按壁纸分文件夹或所有文件放在同一目录
- 🔍 **文件过滤** — 仅提取图片、仅提取模型、排除音频等

### 其他功能
- 🔗 **Steam 集成** — 自动检测 Steam 安装路径和 Workshop 目录
- 📋 **详情面板** — 查看完整元数据、Workshop 链接、标签等
- 🔄 **排序搜索** — 按标题/作者/大小/类型排序，支持关键词搜索
- ⌨️ **快捷键** — Ctrl+A 全选、Ctrl+D 取消全选、Ctrl+F 搜索

---

## 🚀 快速开始

### 系统要求

- Windows 10 1809+ / Windows 11（64-bit）
- .NET 10.0 Runtime（自包含发布版无需安装）

### 安装

1. 从 [Releases](https://github.com/justleeid/RePKG_GUI/releases) 页面下载最新版本
2. 解压或运行安装包
3. 启动 RePKG.Gui.exe

### 开发构建

```bash
# 克隆仓库（含子模块）
git clone --recurse-submodules https://github.com/justleeid/RePKG_GUI.git
cd RePKG_GUI

# 构建
dotnet build

# 运行
dotnet run --project src/RePKG.WpfGui
```

---

## 📖 使用说明

### 基本流程

1. **启动应用** — 自动检测 Steam Workshop 目录
2. **扫描目录** — 点击"扫描检测目录"或"打开目录"选择文件夹
3. **浏览壁纸** — 在网格/列表视图中查看壁纸缩略图和信息
4. **选择壁纸** — 点击卡片选中，支持 Ctrl+A 全选
5. **解包** — 点击"解包选中"，配置选项后开始解包

### 解包选项

| 选项 | 说明 |
|------|------|
| 使用壁纸名称作为子文件夹 | 每个壁纸解包到独立文件夹 |
| 所有文件放在同一个目录下 | 不按壁纸分文件夹 |
| 转换 TEX 为图片 | 自动将 .tex 转换为 PNG（不输出原始 .tex） |
| 仅提取图片 | 只提取图片文件（含转换后的 TEX） |
| 仅提取模型 | 只提取 .obj/.fbx 模型文件 |
| 排除音频 | 排除 .wav/.mp3 等音频文件 |

---

## 🏗️ 技术架构

| 层级 | 技术 | 说明 |
|------|------|------|
| UI 框架 | WPF (.NET 10) | Windows 原生桌面框架 |
| 架构模式 | MVVM | CommunityToolkit.Mvvm |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | DI 容器管理服务生命周期 |
| 日志 | Microsoft.Extensions.Logging | 结构化日志 |
| 解包引擎 | RePKG.Core | 上游 notscuffed/repkg 核心类库（Git Submodule） |
| 目标平台 | Windows 10 1809+ | x64 |

### 性能优化

- **并发扫描** — 使用 SemaphoreSlim 控制并发数，CPU 核心数上限
- **简单可靠** — 网格视图采用标准 ScrollViewer + WrapPanel，稳定可靠
- **缩略图缓存** — 内存 LRU (500张) + 磁盘缓存 (500MB)，SHA256 内容哈希去重
- **异步加载** — 缩略图 fire-and-forget 异步解码，不阻塞 UI

---

## 📁 项目结构

```
RePKG_GUI/
├── README.md
├── LICENSE
├── RePKG.GUI.sln
├── PRDs/                          # 产品/UI/技术文档
│   ├── PRD-产品需求文档.md
│   ├── UI-设计规范与页面布局.md
│   └── TECH-技术文档.md
├── src/RePKG.WpfGui/              # GUI 主项目
│   ├── Models/                    # 数据模型
│   │   ├── WallpaperItem.cs       # 壁纸数据
│   │   ├── ProjectInfo.cs         # project.json 映射
│   │   ├── ExtractOptions.cs      # 解包配置
│   │   └── ExtractProgress.cs     # 进度模型
│   ├── ViewModels/                # MVVM ViewModel 层
│   │   ├── MainViewModel.cs       # 主窗口逻辑
│   │   ├── WallpaperItemViewModel.cs
│   │   ├── MetadataPanelViewModel.cs
│   │   ├── ExtractDialogViewModel.cs
│   │   └── ExtractProgressViewModel.cs
│   ├── Views/                     # XAML 视图
│   │   ├── MainWindow.xaml
│   │   ├── MetadataPanel.xaml
│   │   ├── ExtractDialog.xaml
│   │   ├── ExtractProgressDialog.xaml
│   │   └── Controls/              # 自定义控件
│   │       └── WallpaperCard.xaml
│   ├── Services/                  # 业务逻辑服务
│   │   ├── PkgMetadataService.cs  # 元数据提取
│   │   ├── ExtractService.cs      # 解包服务
│   │   ├── ThumbnailService.cs    # 缩略图缓存
│   │   └── SteamDetectionService.cs
│   └── Converters/                # XAML 值转换器
└── Deps/repkg/                    # 上游 RePKG（Git Submodule）
```

---

## 🤝 致谢

- [notscuffed/repkg](https://github.com/notscuffed/repkg) — RePKG 核心解包引擎（MIT 协议）

## 📄 许可证

本项目基于 [MIT License](LICENSE) 开源。

> ⚠️ **免责声明：** 本工具仅处理用户本地已有的文件，不提供/分发任何版权内容。解包后的素材版权归原作者所有，用户需自行获得授权后使用/再发布。
