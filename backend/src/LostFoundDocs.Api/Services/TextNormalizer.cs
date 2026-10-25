using System.Globalization;
using System.Text;

namespace LostFoundDocs.Api.Services;

public static class TextNormalizer
{
    private static readonly HashSet<string> Particles = new() { "de", "du", "des", "la", "le", "van", "von", "ep", "epse", "nee" };

    /// <summary>Minuscules, sans accents ni ponctuation : « Aïcha  M'barga » → « aicha mbarga » (les apostrophes sont supprimées : N'Guessan = NGuessan).</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark || c == '\'' || c == '’') continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Jetons d'un nom, triés, sans particules : l'ordre « Nom Prénom » / « Prénom Nom » n'a pas d'importance.</summary>
    public static string NormalizeName(string? text) =>
        string.Join(' ', Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !Particles.Contains(t)).OrderBy(t => t, StringComparer.Ordinal));

    public static int Levenshtein(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }

    public static double TokenRatio(string a, string b)
    {
        var max = Math.Max(a.Length, b.Length);
        return max == 0 ? 1 : 1.0 - (double)Levenshtein(a, b) / max;
    }

    /// <summary>Similarité de deux noms normalisés (0 à 1), tolérante aux fautes de frappe et aux prénoms en plus.</summary>
    public static double NameSimilarity(string a, string b)
    {
        var ta = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (ta.Length == 0 || tb.Length == 0) return 0;
        var (small, large) = ta.Length <= tb.Length ? (ta, tb) : (tb, ta);
        var used = new bool[large.Length];
        double sum = 0;
        foreach (var s in small)
        {
            var best = -1;
            var bestRatio = 0.0;
            for (var i = 0; i < large.Length; i++)
            {
                if (used[i]) continue;
                var r = TokenRatio(s, large[i]);
                if (r > bestRatio) { bestRatio = r; best = i; }
            }
            if (best >= 0) { used[best] = true; sum += bestRatio; }
        }
        var penalty = Math.Min(0.2, (large.Length - small.Length) * 0.05);
        return Math.Max(0, sum / small.Length - penalty);
    }

    /// <summary>Similarité de deux textes courts par recouvrement de jetons (organismes émetteurs).</summary>
    public static double TokenOverlap(string a, string b)
    {
        var ta = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var inter = ta.Intersect(tb).Count();
        return (double)inter / Math.Min(ta.Count, tb.Count);
    }
}
