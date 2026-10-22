using MicaComic2PDF.Core.Models;

namespace MicaComic2PDF.Core.Abstractions;

/// <summary>
/// 输出格式模块接口。每个输出格式（PDF / CBZ / ZIP ...）都实现这个接口。
/// Phase 1 里只启用 PDF；CBZ 后续版本再加。
/// </summary>
public interface IOutputFormat
{
    /// <summary>格式唯一 ID，如 "pdf"、"cbz"。</summary>
    string FormatId { get; }

    /// <summary>用户友好的显示名称，如 "PDF 文档"、"CBZ 压缩包"。</summary>
    string DisplayName { get; }

    /// <summary>输出文件扩展名（含点），如 ".pdf"、".cbz"。</summary>
    string FileExtension { get; }

    /// <summary>该格式是否默认启用（UI 里 ComboBox 默认选它）。</summary>
    bool IsDefault { get; }

    /// <summary>
    /// 把预处理好的页面写成目标文件。
    /// </summary>
    /// <param name="pages">已切页+缩放+压缩好的临时 JPEG 页面列表</param>
    /// <param name="outputPathBase">输出路径（不含扩展名，格式实现自己加）</param>
    /// <param name="options">原始转换选项</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>最终生成文件的完整路径</returns>
    Task<string> WriteAsync(
        IReadOnlyList<ImagePage> pages,
        string outputPathBase,
        ConvertOptions options,
        CancellationToken ct = default);
}
