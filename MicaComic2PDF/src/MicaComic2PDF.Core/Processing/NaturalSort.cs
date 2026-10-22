using System.Text.RegularExpressions;

namespace MicaComic2PDF.Core.Processing;

/// <summary>
/// 自然排序 key builder：1, 2, 10 而不是 1, 10, 2。
/// </summary>
public static class NaturalSort
{
    private static readonly Regex _splitRegex = new(@"\d+", RegexOptions.Compiled);

    /// <summary>把文件名拆成交替的字符串和数字段，用于排序比较。</summary>
    public static IEnumerable<object> Key(string name)
    {
        var parts = _splitRegex.Split(name);
        foreach (var p in parts)
        {
            if (string.IsNullOrEmpty(p)) continue;
            if (int.TryParse(p, out var n))
                yield return n;
            else
                yield return p.ToLowerInvariant();
        }
    }

    /// <summary>给定文件路径列表，按文件名自然排序。</summary>
    public static IEnumerable<string> SortByFileName(IEnumerable<string> paths) =>
        paths.OrderBy(p => Key(Path.GetFileName(p)), new NaturalComparer());

    private sealed class NaturalComparer : IComparer<object>
    {
        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            // 数字优先于字符串（保证 2 < 10 当 2 是数字）
            if (x is int xi && y is int yi) return xi.CompareTo(yi);
            if (x is int) return -1;
            if (y is int) return 1;

            return string.CompareOrdinal(x.ToString(), y.ToString());
        }
    }
}
