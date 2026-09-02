using System.Globalization;

namespace AtlasBank.Maui.Converters;

/// <summary>True when the bound reference isn't null – for showing a panel only once
/// something (e.g. CardsViewModel.SelectedCard) has been picked.</summary>
public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
