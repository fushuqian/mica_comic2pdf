namespace MicaComic2PDF.Core.Models;

/// <summary>转换进度报告。</summary>
public readonly record struct ConvertProgress(
    int Current,
    int Total,
    string CurrentImageName);

/// <summary>整本转换的结果。</summary>
public sealed class ConvertResult
{
    public bool Success { get; init; }
    public string? OutputPath { get; init; }
    public string Message { get; init; } = "";
}
