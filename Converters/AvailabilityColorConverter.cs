using System.Globalization;

namespace LolClientHelper.Converters;

/// <summary>
/// Converts an EffectiveAvailability string to its corresponding status Color.
/// Eliminates the need for per-label DataTrigger blocks in CollectionView item templates.
/// </summary>
public sealed class AvailabilityColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string availability)
            return GetDefaultColor();

        return availability.ToLowerInvariant() switch
        {
            "online"   => Color.FromArgb("#22C55E"), // green
            "hosting"  => Color.FromArgb("#22C55E"), // green
            "chat"     => Color.FromArgb("#86EFAC"), // light green
            "ingame"   => Color.FromArgb("#F97316"), // orange
            "away"     => Color.FromArgb("#EF4444"), // red
            "mobile"   => Color.FromArgb("#9CA3AF"), // gray
            "offline"  => Color.FromArgb("#6B7280"), // dark gray
            _          => GetDefaultColor()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Color GetDefaultColor()
    {
        // Fall back to the current theme's default text color
        if (Application.Current?.Resources.TryGetValue("GlassText", out var lightColor) == true
            && Application.Current.Resources.TryGetValue("GlassTextDark", out var darkColor) == true)
        {
            return Application.Current.RequestedTheme == AppTheme.Dark
                ? (Color)darkColor
                : (Color)lightColor;
        }

        return Colors.Gray;
    }
}
