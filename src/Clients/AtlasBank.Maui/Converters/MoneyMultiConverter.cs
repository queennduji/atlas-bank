using System.Globalization;

namespace AtlasBank.Maui.Converters;

/// <summary>Formats an (amount, currency code) pair into something like "$1,234.50". Needs to
/// be a MultiBinding converter because Account/Transaction/Statement store amount and
/// currency as two separate properties, not one pre-formatted string.</summary>
public sealed class MoneyMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [decimal amount, string currency, ..])
        {
            return string.Empty;
        }

        var symbol = currency.ToUpperInvariant() switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => null,
        };

        var formatted = amount.ToString("N2", CultureInfo.InvariantCulture);
        return symbol is null ? $"{formatted} {currency}" : $"{symbol}{formatted}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
