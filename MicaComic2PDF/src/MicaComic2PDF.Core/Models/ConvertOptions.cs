namespace MicaComic2PDF.Core.Models;

/// <summary>转换参数。</summary>
public class ConvertOptions
{
    /// <summary>开页方式，默认右开（日漫）。</summary>
    public SplitMode SplitMode { get; set; } = SplitMode.Right;

    /// <summary>最大边长，0 = 不缩放。默认 2048。</summary>
    public int MaxEdge { get; set; } = 2048;

    /// <summary>JPEG 质量 1-100，默认 85。</summary>
    public int Quality { get; set; } = 85;

    /// <summary>输出目录。留空 = 源文件夹本身。</summary>
    public string OutputDirectory { get; set; } = "";

    /// <summary>输出格式 ID，默认 "pdf"。</summary>
    public string FormatId { get; set; } = "pdf";
}
