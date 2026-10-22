# CHANGELOG

## 2026-10-22（新应用图标）

- AI 生成新图标：浅蓝系可爱动漫少女 wink 形象，脸部大特写填满画面，蓝色书签发饰呼应漫画工具属性
- `assets/app_icon_source.jpg`（1920×1920 源图，保留用于后续多尺寸导出）
- `assets/app_icon_128.png`（128×128 应用图标，高质量双三次缩放）
- v1 奶油色系旧版源图移至 `toberemoved/app_icon_source_v1.jpg`
- 图标定稿并接入项目：生成多尺寸 ICO（16~256），替换 `assets/mica.ico`（exe 图标）与 `App/Assets/AppIcon.ico`（窗口/标题栏图标）；旧版移至 `toberemoved/`
- 需重新构建 WinUI3 项目后生效

## 2026-10-22（WinUI3 启动无响应修复）

- **根本原因**：Unpackaged 模式（EnableMsixTooling=false）未手动初始化 Windows App SDK bootstrap → 进程启动后立即静默崩溃，用户看不到任何反馈
- **修复方式**：csproj 新增 `<WindowsPackageType>None</WindowsPackageType>`，MSBuild 自动处理 bootstrap（之前以为要手写 `Bootstrap.Initialize()`，其实这个项目属性一行搞定）
- **系统依赖**：Unpackaged 模式需要系统安装 Windows App SDK 运行时（已下载安装 2.5.1 x64）
- **教训**：WinUI3 调试时推荐用 `dotnet run -r win-x64` 或者 `dotnet publish -r win-x64 --self-contained` 发布独立 exe

## 2026-10-22（WinUI3 重构 · 模块化输出格式）

- 整体技术栈从 **Python + PySide6** 迁移到 **.NET 9 + WinUI3**
  - 新增 `MicaComic2PDF/` 解决方案目录（4 个 .NET 项目）
  - `src/MicaComic2PDF.Core` — 核心抽象层（不依赖 UI/具体输出格式）
    - Models：SplitMode / ConvertOptions / ImagePage / ConvertProgress
    - Abstractions：**IOutputFormat**（⭐ 可插拔输出格式接口）/ OutputFormatRegistry
    - Processing：NaturalSort / FolderScanner / ImagePreparer
    - ComicPipeline：主编排器（扫描 → 预处理 → 交给选定格式写出）
    - TempDirectory：自动清理临时目录
  - `src/MicaComic2PDF.Formats` — 输出格式模块（PdfOutputFormat + PdfWriter）
    - **自写极简 PDF 1.4 生成器**（~200 行），替代 img2pdf
    - 行为一致：1px = 1pt，JPEG 直嵌，无二次压缩
    - FormatRegistryFactory 一次性注册所有内置格式
  - `src/MicaComic2PDF.Cli` — 命令行（System.CommandLine）
  - `src/MicaComic2PDF.App` — WinUI3 GUI
    - MicaBackdrop 原生背景 + TitleBar 原生标题栏（不再需要自绘 DWM）
    - 左右分栏：左栏设置（含**输出格式 ComboBox**）+ 右栏任务列表和日志
    - CommunityToolkit.Mvvm 源生成器（ObservableProperty / RelayCommand）
    - WinUI3 ListView 拖放文件夹

- 架构亮点
  - **IOutputFormat 模块化**：未来加 CBZ/ZIP/EPUB 只需实现接口 + 注册
  - **Pipeline 分层**：ImagePreparer（切页/缩放/压 JPEG）与输出格式完全解耦
  - SixLabors.ImageSharp 替代 Pillow
  - Windows.UI.Color 替代 System.Drawing.Color
  - WinUI3 Grid 无 IsEnabled → 用 ContentControl 包
  - WinUI3 Window 无 DataContext → 用 RootGrid.Loaded 设

- 旧 Python 版 `comic2pdf.py` 暂时保留，等 WinUI3 版实测 OK 后移到 toberemoved

## 2026-10-22（左右分栏布局 + 自动切页）

- 界面重构：从三段垂直卡片改为左右分栏
  - 窗口 627 → 640 × 700
  - 左栏 200px 固定宽：设置项全部纵向堆叠 + 底部「开始转换」按钮与进度条
  - 右栏：任务列表（上，自适应） + 日志（下，固定 100px）
- 设置区改为「自动切页」checkbox 驱动
- 按钮迁移到任务列表顶部
- 输出目录留空默认从「源文件夹上级」改为「源文件夹内」
- 日志区固定高度 200 → 100px
- 打包：dist\Mica Comic2PDF.exe（58.7 MB）

## 2026-10-22（界面重写 · PySide6）

- Python 版：PySide6 + Win11 Fluent 浅色风格
- 自绘标题栏、DWM 圆角、卡片布局
- exe 打包（dist\Mica Comic2PDF.exe，71.4 MB）

## 2026-10-22（初版）

- Python + Pillow + img2pdf + tkinter 初版
- exe 打包 beta 版（dist\Mica Comic2PDF beta.exe，38.5 MB）
