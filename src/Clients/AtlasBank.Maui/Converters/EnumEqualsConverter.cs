using System.Globalization;

namespace AtlasBank.Maui.Converters;

/// <summary>True when the bound enum's name matches the ConverterParameter string (e.g.
/// "Active") – lets XAML show/hide a control for one specific enum value without a
/// dedicated converter per enum type.</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && parameter is string expected && string.Equals(value.ToString(), expected, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
