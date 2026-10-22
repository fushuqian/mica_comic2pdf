using System.CommandLine;
using MicaComic2PDF.Core;
using MicaComic2PDF.Core.Models;
using MicaComic2PDF.Formats;

namespace MicaComic2PDF.Cli;

/// <summary>
/// 命令行入口。用法：
///   MicaComic2PDF.Cli "D:\漫画\第01卷" --split right --max-edge 2048 --quality 85
///   MicaComic2PDF.Cli folder1 folder2 --format pdf
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // ---- 参数定义 ----
        var foldersArg = new Argument<string[]>("folders", "一个或多个漫画图片文件夹");
        var formatOpt = new Option<string>("--format", "输出格式 ID (pdf / cbz)") { IsRequired = false };
        formatOpt.SetDefaultValue("pdf");

        var splitOpt = new Option<string>("--split", "开页方式: right(右开/日漫) / left(左开) / none(不切页)")
        { IsRequired = false };
        splitOpt.SetDefaultValue("right");
        splitOpt.FromAmong("right", "left", "none");

        var edgeOpt = new Option<int>("--max-edge", "最大边长 (px)，0 = 原图") { IsRequired = false };
        edgeOpt.SetDefaultValue(2048);

        var qualityOpt = new Option<int>("--quality", "JPEG 质量 1-100") { IsRequired = false };
        qualityOpt.SetDefaultValue(85);

        var outOpt = new Option<string>("--out", "输出目录，留空 = 源文件夹内") { IsRequired = false };
        outOpt.SetDefaultValue("");

        var root = new RootCommand("Mica Comic2PDF — 漫画图片文件夹批量转 PDF")
        {
            foldersArg, formatOpt, splitOpt, edgeOpt, qualityOpt, outOpt
        };

        root.SetHandler(async (string[] folders, string formatId, string splitStr,
                                int maxEdge, int quality, string outDir) =>
        {
            var split = splitStr switch
            {
                "left" => SplitMode.Left,
                "none" => SplitMode.None,
                _ => SplitMode.Right
            };

            var options = new ConvertOptions
            {
                SplitMode = split,
                MaxEdge = maxEdge,
                Quality = Math.Clamp(quality, 1, 100),
                OutputDirectory = outDir,
                FormatId = formatId
            };

            // 初始化
            var registry = FormatRegistryFactory.Create();
            var pipeline = new ComicPipeline(registry);

            var failed = 0;
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Console.WriteLine($"[错误] 文件夹不存在: {folder}");
                    failed++;
                    continue;
                }

                try
                {
                    var result = await pipeline.ConvertAsync(
                        folder,
                        options,
                        progress: null,
                        log: Console.WriteLine);

                    if (!result.Success) failed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[异常] {folder}: {ex.Message}");
                    failed++;
                }
            }

            if (folders.Length > 1)
            {
                int ok = folders.Length - failed;
                Console.WriteLine($"\n全部完成：成功 {ok}/{folders.Length}");
            }

        }, foldersArg, formatOpt, splitOpt, edgeOpt, qualityOpt, outOpt);

        return await root.InvokeAsync(args);
    }
}
