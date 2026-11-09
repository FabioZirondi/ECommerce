using System.Globalization;
using System.Text;
using ECommerce.Modules.Products.Domain.Entities;

namespace ECommerce.Modules.Products.Application.Search;

public static class CatalogSearch
{
    private static readonly HashSet<string> StopWords =
    [
        "a", "as", "o", "os", "um", "uma", "de", "da", "do", "das", "dos",
        "e", "em", "na", "no", "nas", "nos", "para", "com", "por"
    ];

    public readonly record struct Match(int Text, int Geo, int Place, string? Hint)
    {
        public int Total => Text + Geo;
        public bool HasText => Text > 0;
    }

    public static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '/' or ',')
            {
                builder.Append(' ');
            }
        }

        var folded = builder.ToString().Normalize(NormalizationForm.FormC);
        while (folded.Contains("  ", StringComparison.Ordinal))
        {
            folded = folded.Replace("  ", " ", StringComparison.Ordinal);
        }

        return folded.Trim();
    }

    public static string[] Tokens(string? query)
    {
        var folded = Fold(query);
        if (folded.Length == 0)
        {
            return [];
        }

        var parts = folded.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var meaningful = parts.Where(part => part.Length >= 2 && !StopWords.Contains(part)).ToArray();
        return meaningful.Length > 0 ? meaningful : parts;
    }

    public static string PlaceLabel(string? city, string? state)
    {
        var place = (city ?? string.Empty).Trim();
        var uf = (state ?? string.Empty).Trim().ToUpperInvariant();
        if (place.Length == 0)
        {
            return uf;
        }

        if (uf.Length == 0 || place.EndsWith(uf, StringComparison.OrdinalIgnoreCase))
        {
            return place;
        }

        return $"{place}, {uf}";
    }

    public static string PlaceKey(string? city, string? state)
    {
        var foldedCity = Fold(city);
        var foldedState = Fold(state);
        if (foldedState.Length > 0)
        {
            var suffix = " " + foldedState;
            if (foldedCity.EndsWith(suffix, StringComparison.Ordinal))
            {
                foldedCity = foldedCity[..^suffix.Length].Trim();
            }
        }

        return $"{foldedCity}|{foldedState}";
    }

    public static bool SamePlace(string? city, string? state, string? nearCity, string? nearState)
    {
        var left = Fold(city);
        var right = Fold(nearCity);
        if (left.Length == 0 || right.Length == 0 || left != right)
        {
            return false;
        }

        var leftState = Fold(state);
        var rightState = Fold(nearState);
        return rightState.Length == 0 || leftState.Length == 0 || leftState == rightState;
    }

    public static bool SameState(string? state, string? nearState)
    {
        var left = Fold(state);
        var right = Fold(nearState);
        return left.Length > 0 && left == right;
    }

    public static int ScoreField(string? field, string? query)
    {
        var text = Fold(field);
        var needle = Fold(query);
        if (text.Length == 0 || needle.Length == 0)
        {
            return 0;
        }

        if (text == needle)
        {
            return 120;
        }

        if (text.StartsWith(needle, StringComparison.Ordinal))
        {
            return 100;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(word => word == needle))
        {
            return 96;
        }

        if (words.Any(word => word.StartsWith(needle, StringComparison.Ordinal)))
        {
            return 88;
        }

        if (text.Contains(needle, StringComparison.Ordinal))
        {
            return 70;
        }

        if (needle.Length >= 4)
        {
            foreach (var word in words)
            {
                if (word.Length < 4 || Math.Abs(word.Length - needle.Length) > 1)
                {
                    continue;
                }

                if (Distance(word, needle) <= 1)
                {
                    return 54;
                }
            }
        }

        return 0;
    }

    public static Match ScoreProduct(
        Product product,
        string categoryName,
        string query,
        string? nearCity,
        string? nearState)
    {
        var phrase = Fold(query);
        if (phrase.Length == 0)
        {
            return default;
        }

        var tokens = Tokens(query);
        var title = Combine(product.Title, phrase, tokens);
        var place = Combine($"{product.City} {product.State}", phrase, tokens);
        var seller = Combine(product.SellerName, phrase, tokens);
        var category = Combine(categoryName, phrase, tokens);
        var description = Fold(product.Description).Contains(phrase, StringComparison.Ordinal) ? 16 : 0;

        var text = new[] { title, Scale(place, 8), Scale(seller, 22), Scale(category, 16), description }.Max();
        var geo = 0;
        if (SamePlace(product.City, product.State, nearCity, nearState))
        {
            geo = 28;
        }
        else if (SameState(product.State, nearState))
        {
            geo = 10;
        }

        string? hint = null;
        if (SamePlace(product.City, product.State, nearCity, nearState))
        {
            hint = "Na sua cidade";
        }
        else if (SameState(product.State, nearState))
        {
            hint = "No seu estado";
        }
        else if (place >= 70)
        {
            hint = "Nesta cidade";
        }
        else if (title is > 0 and < 70)
        {
            hint = "Parecido";
        }

        return new Match(text, geo, place, hint);
    }

    public static int Distance(string left, string right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left.Length == 0)
        {
            return right.Length;
        }

        if (right.Length == 0)
        {
            return left.Length;
        }

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static int Combine(string? field, string phrase, string[] tokens)
    {
        var phraseScore = ScoreField(field, phrase);
        if (tokens.Length <= 1)
        {
            return phraseScore;
        }

        var tokenScores = tokens.Select(token => ScoreField(field, token)).ToArray();
        var hits = tokenScores.Count(score => score > 0);
        if (hits == 0)
        {
            return phraseScore;
        }

        var average = (int)Math.Round(tokenScores.Average());
        if (hits < tokens.Length)
        {
            average -= 12 * (tokens.Length - hits);
        }

        return Math.Max(phraseScore, Math.Max(0, average));
    }

    private static int Scale(int score, int penalty)
        => score <= 0 ? 0 : Math.Max(1, score - penalty);
}
