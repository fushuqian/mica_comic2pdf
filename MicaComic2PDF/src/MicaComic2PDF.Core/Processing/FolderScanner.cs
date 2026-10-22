namespace MicaComic2PDF.Core.Processing;

/// <summary>扫描文件夹，收集支持的图片文件。</summary>
public static class FolderScanner
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff", ".gif"
    };

    /// <summary>只收集文件夹第一层的图片，按自然排序。空文件夹或无法访问时返回空列表。</summary>
    public static IReadOnlyList<string> CollectImages(string folder)
    {
        if (!Directory.Exists(folder)) return Array.Empty<string>();

        var result = new List<string>();
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith('.')) continue;
            if (!ImageExtensions.Contains(Path.GetExtension(name))) continue;

            try { if (new FileInfo(file).Length == 0) continue; }
            catch { continue; }

            result.Add(file);
        }

        NaturalSort.SortByFileName(result);
        return result;
    }

    /// <summary>文件夹名（basename）。</summary>
    public static string FolderName(string folder) =>
        Path.GetFileName(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar));
}
