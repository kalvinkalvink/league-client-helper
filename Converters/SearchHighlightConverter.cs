using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace LolClientHelper.Converters;

public class SearchHighlightConverter : IMultiValueConverter
{
    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        var formattedString = new FormattedString();

        if (values == null || values.Length < 2)
        {
            formattedString.Spans.Add(new Span { Text = values?[0] as string ?? string.Empty });
            return formattedString;
        }

        var text = values[0] as string ?? string.Empty;
        var searchText = values[1] as string ?? string.Empty;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            formattedString.Spans.Add(new Span { Text = text });
            return formattedString;
        }

        var lowerText = text.ToLowerInvariant();
        var lowerSearch = searchText.ToLowerInvariant();
        int currentIndex = 0;

        while (currentIndex < text.Length)
        {
            int matchIndex = lowerText.IndexOf(lowerSearch, currentIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex == -1)
            {
                formattedString.Spans.Add(new Span { Text = text[currentIndex..] });
                break;
            }

            if (matchIndex > currentIndex)
            {
                formattedString.Spans.Add(new Span { Text = text[currentIndex..matchIndex] });
            }

            var matchLength = searchText.Length;
            formattedString.Spans.Add(new Span
            {
                Text = text.Substring(matchIndex, matchLength),
                BackgroundColor = Color.FromArgb("#FFF59D")
            });

            currentIndex = matchIndex + matchLength;
        }

        return formattedString;
    }

    public object[]? ConvertBack(object? value, Type[]? targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
