namespace MicaComic2PDF.Core.Models;

/// <summary>切页 + 缩放 + 压缩完成后的单页数据，存放在临时 JPEG 文件。</summary>
public sealed class ImagePage
{
    /// <summary>临时 JPEG 文件的完整路径。用完会被自动清理。</summary>
    public required string TempPath { get; init; }

    /// <summary>页面宽度（像素）。</summary>
    public int Width { get; init; }

    /// <summary>页面高度（像素）。</summary>
    public int Height { get; init; }
}
