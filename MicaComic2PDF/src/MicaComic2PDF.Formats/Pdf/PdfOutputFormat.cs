using MicaComic2PDF.Core.Abstractions;
using MicaComic2PDF.Core.Models;

namespace MicaComic2PDF.Formats.Pdf;

/// <summary>
/// PDF 输出格式实现。
/// 用自写 PdfWriter 生成 PDF（1px = 1pt，JPEG 直嵌，替代 img2pdf）。
/// </summary>
public sealed class PdfOutputFormat : IOutputFormat
{
    public string FormatId => "pdf";
    public string DisplayName => "PDF 文档";
    public string FileExtension => ".pdf";
    public bool IsDefault => true;

    public async Task<string> WriteAsync(
        IReadOnlyList<ImagePage> pages,
        string outputPathBase,
        ConvertOptions options,
        CancellationToken ct = default)
    {
        var fullPath = outputPathBase + ".pdf";
        using var stream = File.Create(fullPath);
        await PdfWriter.WriteAsync(stream, pages, ct);
        return fullPath;
    }
}
