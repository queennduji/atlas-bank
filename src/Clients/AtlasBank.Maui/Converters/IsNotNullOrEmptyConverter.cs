using System.Globalization;

namespace AtlasBank.Maui.Converters;

/// <summary>True when the bound string is non-null/non-whitespace – used to show error
/// banners and optional detail rows (e.g. Transaction.Description) only when there's something to show.</summary>
public sealed class IsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrWhiteSpace(s);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
