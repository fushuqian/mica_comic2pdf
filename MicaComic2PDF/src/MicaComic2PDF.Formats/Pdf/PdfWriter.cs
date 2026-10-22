using System.Text;
using MicaComic2PDF.Core.Models;

namespace MicaComic2PDF.Formats.Pdf;

/// <summary>
/// 极简 PDF 1.4 写入器：每页嵌入一张 JPEG，1px = 1pt，和 img2pdf 行为一致。
/// 不做压缩/加密/书签——我们只需要"打开就能看"的漫画 PDF。
///
/// PDF 结构：
/// - 每页 3 个 obj（Page + Content Stream + XObject/JPEG）
/// - 加 Catalog(1) + Pages(2)
/// - JPEG 原样嵌入，不二次解码/编码
/// </summary>
public static class PdfWriter
{
    /// <summary>
    /// 把一组已压缩好的 JPEG 页面写成 PDF。
    /// </summary>
    /// <param name="stream">输出流</param>
    /// <param name="pages">每页包含 TempPath（JPEG 文件）和像素宽高</param>
    /// <param name="ct">取消令牌</param>
    public static async Task WriteAsync(
        Stream stream,
        IReadOnlyList<ImagePage> pages,
        CancellationToken ct = default)
    {
        var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        // ---- 收集每页的 JPEG 字节 ----
        var pageImages = new List<(byte[] Jpeg, int Width, int Height)>(pages.Count);
        foreach (var p in pages)
        {
            var jpeg = await File.ReadAllBytesAsync(p.TempPath, ct);
            pageImages.Add((jpeg, p.Width, p.Height));
        }

        // ---- 对象编号规划 ----
        // obj 1: Catalog
        // obj 2: Pages
        // obj 3..2+N: Page （每页 1 个 Page obj）
        // obj 3+N..2+2N: Content Stream（每页 1 个）
        // obj 3+2N..2+3N: XObject/JPEG（每页 1 个）
        var n = pageImages.Count;
        var pageObjBase = 3;
        var contentObjBase = 3 + n;
        var xobjObjBase = 3 + 2 * n;
        var totalObjs = 3 + 3 * n;  // Catalog + Pages + 3*N per page

        // ---- 先写头部（偏移量已知） ----
        // Header
        writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        // Binary comment（某些 PDF 查看器要求）
        writer.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

        // ---- 记录每个对象的偏移量 ----
        var offsets = new long[totalObjs + 1]; // 1-based

        // obj 1: Catalog
        offsets[1] = writer.BaseStream.Position;
        writer.Write(Encoding.ASCII.GetBytes(
            "1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n"));

        // obj 2: Pages
        offsets[2] = writer.BaseStream.Position;
        var kids = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            if (i > 0) kids.Append(' ');
            kids.Append($"{pageObjBase + i} 0 R");
        }
        writer.Write(Encoding.ASCII.GetBytes(
            $"2 0 obj\r\n<< /Type /Pages /Count {n} /Kids [{kids}] >>\r\nendobj\r\n"));

        // 每页：Page obj + Content Stream obj + XObject obj
        for (int i = 0; i < n; i++)
        {
            var (jpeg, pw, ph) = pageImages[i];
            var pageId = pageObjBase + i;
            var contentId = contentObjBase + i;
            var xobjId = xobjObjBase + i;

            // Page obj
            offsets[pageId] = writer.BaseStream.Position;
            // MediaBox 用像素尺寸（1px = 1pt）
            // Resources 引用 XObject，Contents 引用 Content Stream
            writer.Write(Encoding.ASCII.GetBytes(
                $"{pageId} 0 obj\r\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pw} {ph}] " +
                $"/Resources << /XObject << /Im0 {xobjId} 0 R >> >> " +
                $"/Contents {contentId} 0 R >>\r\nendobj\r\n"));

            // Content Stream obj: q 0 W /Im0 Do Q
            // 44 字节正好（加上 stream/endstream 包装更长，算总长度）
            var contentData = Encoding.ASCII.GetBytes("q 0 W /Im0 Do Q");
            offsets[contentId] = writer.BaseStream.Position;
            writer.Write(Encoding.ASCII.GetBytes(
                $"{contentId} 0 obj\r\n<< /Length {contentData.Length} >>\r\nstream\r\n"));
            writer.Write(contentData);
            writer.Write(Encoding.ASCII.GetBytes("\r\nendstream\r\nendobj\r\n"));

            // XObject / JPEG
            offsets[xobjId] = writer.BaseStream.Position;
            writer.Write(Encoding.ASCII.GetBytes(
                $"{xobjId} 0 obj\r\n<< /Type /XObject /Subtype /Image /Width {pw} /Height {ph} " +
                $"/ColorSpace /DeviceRGB /BitsPerComponent 8 " +
                $"/Filter /DCTDecode /Length {jpeg.Length} >>\r\nstream\r\n"));
            writer.Write(jpeg);
            writer.Write(Encoding.ASCII.GetBytes("\r\nendstream\r\nendobj\r\n"));
        }

        // ---- xref 表 ----
        var xrefOffset = writer.BaseStream.Position;
        writer.Write(Encoding.ASCII.GetBytes($"xref\r\n0 {totalObjs}\r\n"));
        writer.Write(Encoding.ASCII.GetBytes("0000000000 65535 f \r\n"));
        for (int i = 1; i <= totalObjs; i++)
        {
            writer.Write(Encoding.ASCII.GetBytes($"{offsets[i]:D10} 00000 n \r\n"));
        }

        // ---- trailer + startxref + %%EOF ----
        writer.Write(Encoding.ASCII.GetBytes(
            $"trailer\r\n<< /Size {totalObjs} /Root 1 0 R >>\r\nstartxref\r\n{xrefOffset}\r\n%%EOF\r\n"));

        writer.Flush();
    }
}
