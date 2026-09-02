using System.Globalization;
using AtlasBank.Clients.Core.Models;

namespace AtlasBank.Maui.Converters;

/// <summary>
/// Maps a status to the same positive/warning/negative/neutral "tone" the web app uses (see
/// frontend/src/lib/badges.ts), then picks a light- or dark-mode color from the same palette
/// as frontend/src/index.css – a "Frozen" account should look the same shade of amber here
/// as it does in the browser.
/// </summary>
public sealed class StatusToColorConverter : IValueConverter
{
    private static readonly Color PositiveLight = Color.FromArgb("#0F8A5F");
    private static readonly Color PositiveDark = Color.FromArgb("#2FD8AE");
    private static readonly Color WarningLight = Color.FromArgb("#B5790A");
    private static readonly Color WarningDark = Color.FromArgb("#E3AC4C");
    private static readonly Color NegativeLight = Color.FromArgb("#D1373F");
    private static readonly Color NegativeDark = Color.FromArgb("#F26D75");
    private static readonly Color NeutralLight = Color.FromArgb("#8A93A1");
    private static readonly Color NeutralDark = Color.FromArgb("#667082");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var tone = value switch
        {
            AccountStatus.Active => Tone.Positive,
            AccountStatus.Frozen => Tone.Warning,
            AccountStatus.Closed => Tone.Neutral,
            TransactionStatus.Completed => Tone.Positive,
            TransactionStatus.Pending => Tone.Warning,
            TransactionStatus.Failed => Tone.Negative,
            CardStatus.Active => Tone.Positive,
            CardStatus.Frozen => Tone.Warning,
            CardStatus.Expired or CardStatus.Cancelled => Tone.Neutral,
            CustomerStatus.Active => Tone.Positive,
            CustomerStatus.Suspended => Tone.Warning,
            CustomerStatus.Closed => Tone.Neutral,
            // TransactionRow.IsCredit: money in reads as positive/green, money out as negative/red.
            bool isCredit => isCredit ? Tone.Positive : Tone.Negative,
            _ => Tone.Neutral,
        };

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return tone switch
        {
            Tone.Positive => isDark ? PositiveDark : PositiveLight,
            Tone.Warning => isDark ? WarningDark : WarningLight,
            Tone.Negative => isDark ? NegativeDark : NegativeLight,
            _ => isDark ? NeutralDark : NeutralLight,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private enum Tone { Positive, Warning, Negative, Neutral }
}
