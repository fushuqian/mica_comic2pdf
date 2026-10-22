# QUEST — 任务清单

## 目标

复刻已绝版的 Windows 小工具 **Pluto Comic2Pdf**：把图片格式的漫画批量转成 PDF，供 iPad / iPhone 阅读。
核心需求（老板原话）：双页扫描的漫画切割成独立两页；转换时可调图片最大尺寸和压缩比例。
**当前架构**：.NET 9 + WinUI3，模块化输出格式（IOutputFormat 可插拔）。

## 需求

- [x] 图形界面，模仿 Pluto Comic2Pdf 的交互
- [x] 开页模式：右开（日漫）/ 左开 / 不切页
- [x] JPEG 质量可调（对应原版"质量 80-100"）
- [x] 最大边长限制（0 = 原图）
- [x] 拖入文件夹自动添加、自动排序、可清空
- [x] 只支持图片文件夹输入（不做 CBZ/CBR 输入）
- [x] 模块化输出格式架构（IOutputFormat），方便扩展 CBZ/ZIP/EPUB

## 已完成

- [x] Python 版完成（PySide6 + Pillow + img2pdf）
- [x] WinUI3 重写（.NET 9 + WinUI 2.3 + CommunityToolkit.Mvvm）
  - Core 层：ComicPipeline + ImagePreparer + FolderScanner + NaturalSort
  - Formats 层：自写极简 PdfWriter（替代 img2pdf，1px=1pt）
  - CLI 层：System.CommandLine 参数解析
  - UI 层：WinUI3 原生 Mica/标题栏/圆角，左右分栏布局
  - 模块化 IOutputFormat 接口，FormatRegistryFactory 注册
- [x] 全部 build 通过（4 个项目 0 错误）

## 待办

- [ ] 老板用真实双页漫画实测（页数、顺序、iPad 全屏、体积）
- [ ] dotnet publish 打包 exe（AOT self-contained，对比 PySide6 71MB）
- [ ] 根据实测反馈调整默认值（最大边长 / 质量）
- [ ] 初始化 git 仓库并按规则同步 GitHub
- [ ] 未来：CBZ 输出格式（接口已就绪，只需实现）

## 明确不做

- CBZ/CBR 输入（输出可以加，输入不做）
- PDF 加密、书签、目录
- 图片去噪 / 自动裁白边
- 多语言 / 本地化
