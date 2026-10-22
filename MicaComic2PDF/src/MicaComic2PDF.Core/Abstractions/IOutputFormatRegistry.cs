namespace MicaComic2PDF.Core.Abstractions;

/// <summary>输出格式注册表，提供格式查询能力。</summary>
public interface IOutputFormatRegistry
{
    /// <summary>返回所有已注册的格式（按 IsDefault 优先 + FormatId 排序）。</summary>
    IReadOnlyList<IOutputFormat> GetAll();

    /// <summary>按 ID 获取格式，找不到抛 <see cref="KeyNotFoundException"/>。</summary>
    IOutputFormat Get(string formatId);

    /// <summary>获取默认格式。</summary>
    IOutputFormat GetDefault();

    /// <summary>按 ID 尝试获取。</summary>
    bool TryGet(string formatId, out IOutputFormat? format);
}

/// <summary>
/// 简单的基于内存字典的注册表实现。
/// </summary>
public sealed class OutputFormatRegistry : IOutputFormatRegistry
{
    private readonly Dictionary<string, IOutputFormat> _formats = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultId = "pdf";

    /// <summary>手动注册一个格式。</summary>
    public void Register(IOutputFormat format)
    {
        _formats[format.FormatId] = format;
        if (format.IsDefault) _defaultId = format.FormatId;
    }

    /// <summary>按类型注册（方便 DI 或显式注册）。</summary>
    public void Register<T>() where T : IOutputFormat, new() => Register(new T());

    public IReadOnlyList<IOutputFormat> GetAll() =>
        _formats.Values
            .OrderByDescending(f => f.IsDefault)
            .ThenBy(f => f.FormatId)
            .ToList();

    public IOutputFormat Get(string formatId) =>
        _formats.TryGetValue(formatId, out var f)
            ? f
            : throw new KeyNotFoundException($"未注册的输出格式: {formatId}");

    public IOutputFormat GetDefault() =>
        _formats.TryGetValue(_defaultId, out var f)
            ? f
            : _formats.Values.First();

    public bool TryGet(string formatId, out IOutputFormat? format) =>
        _formats.TryGetValue(formatId, out format);
}
