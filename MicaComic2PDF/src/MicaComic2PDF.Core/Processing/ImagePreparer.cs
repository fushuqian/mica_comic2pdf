using MicaComic2PDF.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MicaComic2PDF.Core.Processing;

/// <summary>
/// 单张图片预处理：EXIF 摆正 → 按 SplitMode 切页 → 缩放（只缩不放）→ 压成 JPEG 到临时文件。
/// 和输出格式无关，所有格式共用。
/// </summary>
public static class ImagePreparer
{
    /// <summary>宽/高 超过该阈值视为跨页，与 Python 版 SPLIT_RATIO=1.2 一致。</summary>
    private const double SplitRatio = 1.2;

    /// <summary>临时目录前缀。</summary>
    public const string TempDirPrefix = "mica_c2p_";

    /// <summary>
    /// 单张图片处理，返回 1~2 个临时 JPEG 文件路径。
    /// </summary>
    public static async Task<IReadOnlyList<ImagePage>> PrepareAsync(
        string srcPath,
        ConvertOptions options,
        string tempDir,
        int pageNo,
        CancellationToken ct = default)
    {
        // 加载为具体像素格式，ImageSharp 3.x 的泛型 Image<TPixel> 支持 Clone
        using var raw = await Image.LoadAsync<Rgba32>(srcPath, ct);

        // 1) EXIF 摆正
        raw.Mutate(x => x.AutoOrient());

        var pages = new List<ImagePage>(2);
        var (w, h) = (raw.Width, raw.Height);

        // 2) 切页（宽高比 > 1.2 且 SplitMode != None）
        List<Image<Rgba32>> pieces;
        if (options.SplitMode != SplitMode.None && h > 0 && (double)w / h > SplitRatio)
        {
            var mid = w / 2;
            var left = raw.Clone();
            left.Mutate(x => x.Crop(new Rectangle(0, 0, mid, h)));
            var right = raw.Clone();
            right.Mutate(x => x.Crop(new Rectangle(mid, 0, w - mid, h)));

            pieces = options.SplitMode == SplitMode.Right
                ? new List<Image<Rgba32>> { right, left }
                : new List<Image<Rgba32>> { left, right };
        }
        else
        {
            pieces = new List<Image<Rgba32>> { raw.Clone() };
        }

        try
        {
            for (var i = 0; i < pieces.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var piece = pieces[i];

                // 3) 缩放（只缩不放，长边不超过 MaxEdge）
                var pieceW = piece.Width;
                var pieceH = piece.Height;
                if (options.MaxEdge > 0)
                {
                    var longEdge = Math.Max(pieceW, pieceH);
                    if (longEdge > options.MaxEdge)
                    {
                        var scale = (double)options.MaxEdge / longEdge;
                        var newW = Math.Max(1, (int)Math.Round(pieceW * scale));
                        var newH = Math.Max(1, (int)Math.Round(pieceH * scale));
                        piece.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(newW, newH),
                            Mode = ResizeMode.Stretch,
                            Sampler = KnownResamplers.Lanczos3,
                            Compand = true
                        }));
                        pieceW = piece.Width;
                        pieceH = piece.Height;
                    }
                }

                // 4) 保存 JPEG
                var tempPath = System.IO.Path.Combine(tempDir, $"{pageNo:05d}_{i}.jpg");
                await SaveJpegAsync(piece, tempPath, options.Quality, ct);

                pages.Add(new ImagePage
                {
                    TempPath = tempPath,
                    Width = pieceW,
                    Height = pieceH
                });

                piece.Dispose();
            }
        }
        finally
        {
            foreach (var p in pieces) p.Dispose();
        }

        return pages;
    }

    /// <summary>保存 JPEG，质量 1-100。Rgba32 的 Alpha 通道会被 JpegEncoder 自动忽略。</summary>
    private static async Task SaveJpegAsync(Image<Rgba32> image, string path, int quality, CancellationToken ct)
    {
        quality = Math.Clamp(quality, 1, 100);
        var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
        {
            Quality = quality
        };
        await image.SaveAsJpegAsync(path, encoder, ct);
    }
}
