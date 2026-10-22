# IMPORTANT

## 项目位置

- **本项目**：`D:\Trae\Mica Comic2PDF`
- **旧 Python 版**：`comic2pdf.py`（暂时保留，等 WinUI3 版实测 OK 后移 toberemoved）
- **.NET 解决方案**：`MicaComic2PDF/MicaComic2PDF.sln`
- **图标**：`assets\mica.ico`

## 环境

- **.NET SDK**：9.0.319
- **WinUI3**：Microsoft.WindowsAppSDK 2.3.9（模板版本）
- **Target Framework**：`net9.0-windows10.0.19041.0`（Win10 21H2+）
- **架构**：x64
- **模板安装**：`dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`

## 项目结构

```
MicaComic2PDF/
├── src/
│   ├── MicaComic2PDF.Core/        # 核心抽象层（无 UI / 无具体输出格式）
│   │   ├── Models/                 # SplitMode / ConvertOptions / ImagePage / ConvertProgress
│   │   ├── Abstractions/           # IOutputFormat / OutputFormatRegistry（⭐ 可插拔）
│   │   ├── Processing/             # NaturalSort / FolderScanner / ImagePreparer
│   │   ├── ComicPipeline.cs        # 主编排器
│   │   └── TempDirectory.cs        # 自动清理临时目录
│   ├── MicaComic2PDF.Formats/     # 输出格式模块（可插拔扩展）
│   │   ├── Pdf/
│   │   │   ├── PdfOutputFormat.cs  # 实现 IOutputFormat
│   │   │   └── PdfWriter.cs        # 自写极简 PDF 1.4 二进制
│   │   └── FormatRegistryFactory.cs
│   ├── MicaComic2PDF.Cli/         # 命令行
│   │   └── Program.cs              # System.CommandLine 解析
│   └── MicaComic2PDF.App/         # WinUI3 GUI
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── ViewModels/MainViewModel.cs   # CommunityToolkit.Mvvm 源生成器
│       └── Converters/Converters.cs
```

## 依赖

| 项目 | 包 | 版本 |
|------|------|------|
| Core | SixLabors.ImageSharp | 3.1.6 |
| Cli | System.CommandLine | 2.0.0-beta4.22272.1 |
| App | Microsoft.WindowsAppSDK | 2.3.9 (模板默认) |
| App | CommunityToolkit.Mvvm | 8.3.2 |

## 关键实现

### PdfWriter（替代 img2pdf）
- 极简 PDF 1.4，每页嵌入一张 JPEG（DCTDecode）
- 1px = 1pt（MediaBox = [0 0 W H]，无二次压缩）
- xref + trailer + %%EOF 完整二进制结构
- 每页 3 个 obj（Page + Content Stream + XObject/JPEG）

### ImagePreparer（替代 Pillow）
- SixLabors.ImageSharp LoadAsync<Rgba32>()
- AutoOrient（EXIF 摆正）
- SplitRatio = 1.2（宽/高超过视为跨页）
- Left.Clone / Right.Clone（Image<Rgba32>.Clone）
- ResizeOptions { Mode=Stretch, Sampler=Lanczos3, Compand=true }
- JpegEncoder.Quality（≥90 自动用 4:4:4）

### 模块化输出格式
- IOutputFormat 接口：FormatId / DisplayName / FileExtension / IsDefault / WriteAsync
- OutputFormatRegistry：手动 Register 或反射 Scan
- FormatRegistryFactory：一次性注册所有内置格式
- **扩展步骤**：新建类实现接口 + 加 [Format] 特性 + Register → GUI/CLI 自动可用

## 构建命令

```powershell
# Build
dotnet build MicaComic2PDF/MicaComic2PDF.sln

# Run CLI
dotnet run --project MicaComic2PDF/src/MicaComic2PDF.Cli -- "D:\漫画\第01卷" --format pdf --split right --max-edge 2048 --quality 85

# WinUI3 publish（AOT + self-contained）
dotnet publish MicaComic2PDF/src/MicaComic2PDF.App/MicaComic2PDF.App.csproj `
  -c Release -r win-x64 --self-contained true /p:PublishAot=true `
  /p:ApplicationIcon=..\..\..\assets\mica.ico
```

## WinUI3 踩坑记录

- **Window 没有 Width/Height** → 用 `AppWindow.Resize(new SizeInt32(820, 640))`
- **Window 没有 DataContext** → 在 XAML 根 Grid 上设 `Loaded` 事件赋值
- **Grid/StackPanel 没有 IsEnabled** → 用 ContentControl 包
- **ProgressBar 没有 ShowErrorState/ShowPausedState** → 简化不用
- **Slider 没有 IsSnapToTickEnabled** → 简化不用
- **Binding 没有 StringFormat** → 用 Converter 或 ViewModel 属性
- **Binding 没有 CommandParameter** → Button.CommandParameter 直接设
- **WinUI Colors 在 Windows.UI 命名空间** → `Microsoft.UI.Colors.Green`
- **Image<Rgba32>.Clone()** 在 ImageSharp 3.x 仍可用（泛型 Image 有）
- **MicaBackdrop Kind="BaseAlt"** 在远程环境可能仍不出效果（但至少原生标题栏/圆角 OK）

## PDF 结构（PdfWriter 生成）

```
%PDF-1.4\n%二进制注释

1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj
2 0 obj << /Type /Pages /Count N /Kids [3 0 R ...] >> endobj

每页 3 个 obj：
  Page obj    → MediaBox [0 0 W H]（1px = 1pt）
  Content obj → stream: q 0 W /Im0 Do Q
  XObject obj → /Filter /DCTDecode + JPEG 原始字节

xref + trailer + startxref + %%EOF
```

iPad 打开满屏无白边（1pt = 1/72 inch ≈ 0.35mm，iPad 300DPI 上 3.43pt = 2.54mm，刚好 1 物理像素点）。
