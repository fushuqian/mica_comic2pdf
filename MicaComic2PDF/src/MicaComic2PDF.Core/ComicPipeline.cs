using System.Collections.Concurrent;
using MicaComic2PDF.Core.Abstractions;
using MicaComic2PDF.Core.Models;
using MicaComic2PDF.Core.Processing;

namespace MicaComic2PDF.Core;

/// <summary>
/// 漫画文件夹 → 目标输出 的主编排器。
/// 负责：扫描 → 预处理（切页/缩放/压JPEG） → 交给选定的 IOutputFormat 写出。
/// </summary>
public sealed class ComicPipeline
{
    private readonly IOutputFormatRegistry _formats;

    public ComicPipeline(IOutputFormatRegistry formats)
    {
        _formats = formats;
    }

    /// <summary>
    /// 转换单个漫画文件夹。
    /// </summary>
    /// <param name="folder">图片文件夹路径</param>
    /// <param name="options">转换选项（包含输出格式 ID）</param>
    /// <param name="progress">进度回调</param>
    /// <param name="log">日志回调</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>转换结果（成功 = 输出路径；失败 = null + Message）</returns>
    public async Task<ConvertResult> ConvertAsync(
        string folder,
        ConvertOptions options,
        IProgress<ConvertProgress>? progress = null,
        Action<string>? log = null,
        CancellationToken ct = default)
    {
        folder = Path.GetFullPath(folder);

        // 1) 扫描图片
        var images = FolderScanner.CollectImages(folder);
        if (images.Count == 0)
        {
            log?.Invoke($"[跳过] {FolderScanner.FolderName(folder)}：没有找到图片");
            return new ConvertResult { Success = false, Message = "没有找到图片" };
        }

        // 2) 准备输出目录 & 路径
        var outDir = string.IsNullOrEmpty(options.OutputDirectory)
            ? folder
            : Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outDir);

        // 避免重名覆盖
        var baseName = FolderScanner.FolderName(folder);
        var outputPathBase = UniqueBasePath(outDir, baseName);

        // 3) 创建临时目录（所有临时文件放在这，using 结束自动清）
        using var tempDir = new TempDirectory(ImagePreparer.TempDirPrefix);

        // 4) 预处理（和输出格式无关）
        var allPages = new List<ImagePage>(images.Count * 2);
        var failedCount = 0;

        for (int i = 0; i < images.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var pageNo = i + 1;
                var pages = await ImagePreparer.PrepareAsync(
                    images[i], options, tempDir.FullPath, pageNo, ct);
                allPages.AddRange(pages);
            }
            catch (Exception ex)
            {
                failedCount++;
                log?.Invoke($"[失败] {Path.GetFileName(images[i])}：{ex.Message}");
            }

            progress?.Report(new ConvertProgress(i + 1, images.Count, Path.GetFileName(images[i])));
        }

        if (allPages.Count == 0)
        {
            log?.Invoke($"[失败] {FolderScanner.FolderName(folder)}：没有可用页面");
            return new ConvertResult { Success = false, Message = "没有可用页面" };
        }

        // 5) 交给选定的输出格式
        var format = _formats.Get(options.FormatId);
        log?.Invoke($"[输出] 使用格式: {format.DisplayName}");

        string finalPath;
        try
        {
            finalPath = await format.WriteAsync(allPages, outputPathBase, options, ct);
        }
        catch (Exception ex)
        {
            log?.Invoke($"[异常] 写出失败：{ex.Message}");
            return new ConvertResult { Success = false, Message = ex.Message };
        }

        // 6) 汇报结果
        var sizeMB = new FileInfo(finalPath).Length / 1048576.0;
        log?.Invoke($"[完成] {Path.GetFileName(finalPath)}：{images.Count} 张图 → " +
                    $"{allPages.Count} 页，{sizeMB:F1} MB" +
                    (failedCount > 0 ? $"（{failedCount} 张失败）" : ""));

        return new ConvertResult
        {
            Success = true,
            OutputPath = finalPath,
            Message = $"成功，{allPages.Count} 页"
        };
    }

    /// <summary>
    /// 生成不重复的输出路径 base（避免覆盖已有文件）。
    /// 不关心扩展名——由输出格式自己加。
    /// </summary>
    private static string UniqueBasePath(string outDir, string baseName)
    {
        // 先用 .pdf 试探（最常见的），如果存在就加 _1、_2
        // 这里不管扩展名，只是防止 base 冲突
        var candidate = Path.Combine(outDir, baseName);
        if (!AnyFileExists(candidate, ".pdf", ".cbz", ".zip", ".epub"))
            return candidate;

        int i = 1;
        while (AnyFileExists($"{candidate}_{i}", ".pdf", ".cbz", ".zip", ".epub"))
            i++;

        return $"{candidate}_{i}";
    }

    private static bool AnyFileExists(string basePath, params string[] exts) =>
        exts.Any(ext => File.Exists(basePath + ext));
}
