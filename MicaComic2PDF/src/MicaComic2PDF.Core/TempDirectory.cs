namespace MicaComic2PDF.Core;

/// <summary>
/// 临时目录，using 结束自动清空并删除。
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public string FullPath { get; }

    public TempDirectory(string prefix)
    {
        FullPath = Directory.CreateDirectory(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"))).FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(FullPath, recursive: true); }
        catch { /* 忽略清理失败 */ }
    }
}
