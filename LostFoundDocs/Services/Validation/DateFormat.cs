using System.Globalization;

namespace LostFoundDocs.Services.Validation;

/// <summary>Formats de date français uniques pour toute l'application.</summary>
public static class DateFormat
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public static string Short(DateTime d) => d.ToString("dd MMM yyyy", Fr);     // 02 oct. 2026
    public static string Long(DateTime d) => d.ToString("dd MMMM yyyy", Fr);     // 02 octobre 2026
    public static string Time(DateTime d) => d.ToString("HH:mm", Fr);            // 10:24

    /// <summary>« il y a 12 min », « il y a 3 h », « hier », « il y a 3 jours », sinon date courte.</summary>
    public static string Ago(DateTime then, DateTime now)
    {
        var span = now - then;
        if (span < TimeSpan.FromMinutes(1)) return "à l’instant";
        if (span < TimeSpan.FromHours(1)) return $"il y a {(int)span.TotalMinutes} min";
        if (then.Date == now.Date) return $"il y a {(int)span.TotalHours} h";
        if (then.Date == now.Date.AddDays(-1)) return "hier";
        if (span < TimeSpan.FromDays(30)) return $"il y a {(int)span.TotalDays} jours";
        return Short(then);
    }

    /// <summary>Même résultat avec une majuscule initiale (« Il y a 5 min »).</summary>
    public static string AgoCapitalized(DateTime then, DateTime now)
    {
        var s = Ago(then, now);
        return char.ToUpper(s[0], Fr) + s[1..];
    }
}
