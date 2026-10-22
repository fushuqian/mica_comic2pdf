namespace MicaComic2PDF.Core.Models;

/// <summary>开页方式。</summary>
public enum SplitMode
{
    /// <summary>不切页。</summary>
    None,
    /// <summary>左开（标准翻页，左半页在前）。</summary>
    Left,
    /// <summary>右开（日式翻页，右半页在前）。</summary>
    Right,
}
