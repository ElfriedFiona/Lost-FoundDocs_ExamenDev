using System.Globalization;
using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Validation;

/// <summary>Lieux publics recommandés et créneaux proposables (toujours dans le futur, calculés à partir de la date du jour).</summary>
public static class RestitutionRules
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public static readonly IReadOnlyList<MeetingPlace> Places = new List<MeetingPlace>
    {
        new("Commissariat du 11e arrondissement", "Bonamoussadi"),
        new("Hôtel de ville de Douala 1er", "Bonanjo"),
        new("Entrée principale, Université de Douala", "Campus 1"),
    };

    public static readonly IReadOnlyList<string> Hours = new List<string> { "09:00", "10:00", "14:00", "16:00" };

    /// <summary>Trois jours à partir de demain : « Demain, 19 oct. », « Mar. 20 oct. », « Mer. 21 oct. ».</summary>
    public static IReadOnlyList<(string Label, DateTime Date)> Days(DateTime today)
    {
        var list = new List<(string, DateTime)>();
        for (var i = 1; i <= 3; i++)
        {
            var d = today.Date.AddDays(i);
            var label = i == 1
                ? $"Demain, {d.ToString("d MMM", Fr)}"
                : Capitalize(d.ToString("ddd d MMM", Fr));
            list.Add((label, d));
        }
        return list;
    }

    public static DateTime Combine(DateTime day, string hour) =>
        day.Date.Add(TimeSpan.ParseExact(hour, "hh\\:mm", CultureInfo.InvariantCulture));

    private static string Capitalize(string s) => char.ToUpper(s[0], Fr) + s[1..];
}
