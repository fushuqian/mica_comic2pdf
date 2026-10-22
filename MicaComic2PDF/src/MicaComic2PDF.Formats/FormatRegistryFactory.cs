using MicaComic2PDF.Core.Abstractions;
using MicaComic2PDF.Formats.Pdf;

namespace MicaComic2PDF.Formats;

/// <summary>
/// 格式注册表工厂 —— 一次性注册所有内置格式。
/// CLI 和 App 都调用这个来拿到初始化好的注册表。
/// </summary>
public static class FormatRegistryFactory
{
    public static OutputFormatRegistry Create()
    {
        var registry = new OutputFormatRegistry();
        registry.Register(new PdfOutputFormat());
        // 以后加格式：registry.Register(new CbzOutputFormat());
        return registry;
    }
}
