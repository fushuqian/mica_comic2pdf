using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MicaComic2PDF.App.Converters;

/// <summary>bool → 取反。IsConverting = true 时按钮禁用。</summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : true;
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : false;
}

/// <summary>任务状态 → 对应颜色。</summary>
public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string status)
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);

        var c = status switch
        {
            "完成" => Microsoft.UI.Colors.Green,
            "失败" => Microsoft.UI.Colors.Red,
            "异常" => Microsoft.UI.Colors.Red,
            "跳过" => Windows.UI.Color.FromArgb(0xFF, 0x8A, 0x6D, 0x1F),
            "转换中..." => Windows.UI.Color.FromArgb(0xFF, 0x2F, 0x6B, 0xFF),
            _ => Windows.UI.Color.FromArgb(0xFF, 0x5B, 0x64, 0x72),
        };
        return new SolidColorBrush(c);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
