# RePKG GUI

> Wallpaper Engine PKG/TEX 解包工具 — 图形界面版

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-lightgrey.svg)]()

**RePKG GUI** 是一款面向 Wallpaper Engine 普通用户的 Windows 桌面工具。核心价值是：**在不解包全部文件的前提下，以可视化列表（缩略图 + 元数据）方式浏览 PKG 壁纸内容，并支持按需选择性解包。**

---

## ✨ 功能特性

- 🔍 **可视化浏览** — 网格/列表视图展示壁纸缩略图、标题、作者、类型
- 📊 **元数据预览** — 不解包即可查看 project.json 中的标题、标签、大小等信息
- ✂️ **选择性解包** — 勾选想要的壁纸，一键解包到指定目录
- 🖼️ **TEX 转换** — 自动将 .tex 纹理文件转换为 PNG 图片
- 🔗 **Steam 集成** — 自动检测 Steam 安装路径和 Workshop 目录
- 🎯 **零学习成本** — 无需了解 CLI 参数，纯图形界面操作

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

## 🏗️ 技术架构

| 层级 | 技术 | 说明 |
|------|------|------|
| UI 框架 | WPF (.NET 10) | Windows 原生桌面框架 |
| 架构模式 | MVVM | CommunityToolkit.Mvvm |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | DI 容器管理服务生命周期 |
| 解包引擎 | RePKG.Core | 上游 notscuffed/repkg 核心类库（Git Submodule） |
| 目标平台 | Windows 10 1809+ | x64 |

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
│   ├── ViewModels/                # MVVM ViewModel 层
│   ├── Views/                     # XAML 视图
│   │   └── Controls/              # 自定义控件
│   ├── Services/                  # 业务逻辑服务
│   ├── Converters/                # XAML 值转换器
│   └── Resources/                 # 静态资源
└── Deps/repkg/                    # 上游 RePKG（Git Submodule）
```

## 🤝 致谢

- [notscuffed/repkg](https://github.com/notscuffed/repkg) — RePKG 核心解包引擎（MIT 协议）

## 📄 许可证

本项目基于 [MIT License](LICENSE) 开源。

> ⚠️ **免责声明：** 本工具仅处理用户本地已有的文件，不提供/分发任何版权内容。解包后的素材版权归原作者所有，用户需自行获得授权后使用/再发布。
