# Mica Comic2PDF

复刻已绝版的 Windows 小工具 **Pluto Comic2Pdf**：把图片格式的漫画批量转成 PDF，供 iPad / iPhone 阅读。

## ✨ 功能

- 📁 图片文件夹拖入即添加，自动自然排序
- ✂️ **自动切页**：双页扫描漫画一键切成独立两页
  - 日式翻页（右开）
  - 标准翻页（左开）
- 🎚️ **可调参数**
  - 最大边长（0 = 原图）
  - JPEG 质量（1–100）
- 🖨️ 输出 PDF 1.4，JPEG 直嵌不二次压缩（体积小、画质好）
- 🎨 Windows 11 原生 Mica 风格界面（WinUI 3）
- 🔌 模块化输出格式架构，轻松扩展 CBZ / EPUB

## 🛠️ 技术栈

| 项 | 说明 |
|---|---|
| 语言 | C# 12 / .NET 9 |
| UI 框架 | WinUI 3 (Windows App SDK 2.5) |
| MVVM | CommunityToolkit.Mvvm 源生成器 |
| 图像处理 | SixLabors.ImageSharp 3 |
| PDF 生成 | 自写极简 PDF 1.4（~200 行，JPEG 直嵌） |
| CLI | System.CommandLine |
| 目标平台 | Windows 10 21H2+ (x64) |

## 📦 下载

👉 单文件 EXE（拷到哪里都能跑，137 MB）：

[dist/singlefile/MicaComic2PDF.App.exe](dist/singlefile/MicaComic2PDF.App.exe)

无需安装 .NET Runtime 或 Windows App SDK。

## 🚀 快速开始

### GUI

```powershell
# 双击运行单文件 EXE，或开发态：
dotnet run --project MicaComic2PDF/src/MicaComic2PDF.App -r win-x64
```

### CLI

```powershell
dotnet run --project MicaComic2PDF/src/MicaComic2PDF.Cli -- "D:\漫画\第01卷" --split right --max-edge 2048 --quality 85
```

## 🏗️ 从源码构建

### 环境要求

- .NET SDK 9.0+
- Windows 10 21H2+ (22H2 / Win11 推荐)
- Visual Studio 2022 17.8+（可选，用 VS Code + `dotnet` CLI 也行）

### 构建步骤

```powershell
# 克隆
git clone https://github.com/fushuqian/mica_comic2pdf.git
cd mica_comic2pdf

# 还原 + 编译
dotnet build MicaComic2PDF/MicaComic2PDF.sln

# 发布单文件 EXE
dotnet publish MicaComic2PDF/src/MicaComic2PDF.App -c Release -r win-x64 -o dist/singlefile
```

## 📂 项目结构

```
MicaComic2PDF/
├── src/
│   ├── MicaComic2PDF.Core/         # 核心抽象层（无 UI 依赖）
│   │   ├── Abstractions/           # IOutputFormat 接口
│   │   ├── Models/                 # SplitMode / ConvertOptions ...
│   │   ├── Processing/             # FolderScanner / ImagePreparer / NaturalSort
│   │   └── ComicPipeline.cs        # 主编排器
│   ├── MicaComic2PDF.Formats/      # 输出格式模块
│   │   └── Pdf/PdfWriter.cs        # 自写 PDF 生成器
│   ├── MicaComic2PDF.Cli/          # 命令行入口
│   └── MicaComic2PDF.App/          # WinUI 3 GUI
└── MicaComic2PDF.sln
```

### 核心架构

```
ComicPipeline（编排）
  ├─ FolderScanner       扫目录 + 自然排序
  ├─ ImagePreparer       切页 → 缩放 → 压 JPEG
  └─ IOutputFormat       交给选定格式写出
       └─ PdfOutputFormat → PdfWriter
```

**模块化**：未来要加 CBZ / EPUB？实现 `IOutputFormat` 接口 + 在 `FormatRegistryFactory` 注册即可，Core 层不用改一行。

## 🧩 架构亮点

- **IOutputFormat 解耦**：图片预处理与输出格式完全分离
- **1px = 1pt**：PDF 页面尺寸直接用像素值，iPad / iPhone 原生阅读器全屏无黑边
- **JPEG 直嵌 PDF**：`/Filter /DCTDecode` 不二次解码/编码，体积与 img2pdf 一致
- **NaturalSort 自然排序**：第 1 页、第 2 页……第 10 页，而不是第 1、10、2

## 📜 许可证

MIT License
