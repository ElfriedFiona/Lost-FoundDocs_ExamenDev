using System.Text.RegularExpressions;
using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// Vérifie la cohérence chronologique et logique des données fictives.
/// Exécuté au démarrage en Debug (résultat dans la fenêtre Sortie) et réutilisable dans des tests.
/// </summary>
public static class DemoDataValidator
{
    private static readonly Regex ReferencePattern = new(@"^LF-\d{4}-\d{6}$", RegexOptions.Compiled);

    public static IReadOnlyList<string> Validate(IEnumerable<Report> reports, DateTime now)
    {
        var problems = new List<string>();
        var list = reports.ToList();

        foreach (var r in list)
        {
            string Tag(string text) => $"[{r.Reference}] {text}";

            if (!ReferencePattern.IsMatch(r.Reference)) problems.Add(Tag("référence au mauvais format"));
            if (r.EventDate.Date > now.Date) problems.Add(Tag("date d’événement dans le futur"));
            if (r.CreatedAt < r.EventDate.Date) problems.Add(Tag("déclaré avant l’événement"));
            if (r.CreatedAt > now) problems.Add(Tag("créé dans le futur"));
            if (r.UpdatedAt < r.CreatedAt) problems.Add(Tag("mis à jour avant sa création"));
            if (r.UpdatedAt > now) problems.Add(Tag("mis à jour dans le futur"));
            if (r.BirthDate is { } b && b.Date >= r.EventDate.Date) problems.Add(Tag("naissance postérieure à l’événement"));

            var age = now - r.CreatedAt;
            if (r.Status == ReportStatus.Returned && r.UpdatedAt <= r.CreatedAt) problems.Add(Tag("restitué sans délai après la déclaration"));
            if (r.Status == ReportStatus.Expired && r.UpdatedAt < r.CreatedAt.AddDays(ReportStatusRules.ExpiryDays)) problems.Add(Tag("expiré avant 90 jours"));
            if ((r.Status is ReportStatus.Searching or ReportStatus.MatchFound) && age.TotalDays > ReportStatusRules.ExpiryDays)
                problems.Add(Tag("signalement actif plus ancien que 90 jours"));
        }

        foreach (var dup in list.GroupBy(r => r.Reference).Where(g => g.Count() > 1))
            problems.Add($"[{dup.Key}] référence en double");
        foreach (var dup in list.GroupBy(r => r.Id).Where(g => g.Count() > 1))
            problems.Add($"[{dup.Key}] identifiant en double");

        return problems;
    }
}
