using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// SIMULATION (frontend uniquement) : jeu de données fictif du compte de démonstration.
/// Toutes les dates sont calculées par rapport à « maintenant » (jamais de date figée) et respectent
/// les invariants vérifiés par <see cref="DemoDataValidator"/>.
/// </summary>
public static class DemoData
{
    public static List<Report> CreateReportsFor(Guid userId, DateTime now)
    {
        var today = now.Date;
        var year = now.Year;
        string Ref(int n) => $"LF-{year}-{n:D6}";

        // Date de l'événement = today - daysAgo ; déclaration le jour même (à l'heure indiquée) ou plus tard.
        Report Make(ReportKind kind, DocumentType type, int daysAgo, int declaredHour, string place,
                    ReportStatus status, double statusAfterDays, int refNumber, string? number = null)
        {
            var eventDate = today.AddDays(-daysAgo);
            var created = eventDate.AddHours(declaredHour);
            var updated = created.AddDays(statusAfterDays);
            if (updated > now) updated = now.AddMinutes(-5);
            if (updated < created) updated = created;
            return new Report
            {
                UserId = userId,
                Reference = Ref(refNumber),
                Kind = kind,
                Type = type,
                HolderName = DemoSettings.DemoFullName,
                Number = number,
                EventDate = eventDate,
                City = "Douala",
                Place = place,
                CreatedAt = created,
                UpdatedAt = updated,
                Status = status,
            };
        }

        return new List<Report>
        {
            Make(ReportKind.Lost, DocumentType.NationalId, 5, 18, "Université de Douala, campus 1", ReportStatus.MatchFound, 1, 3317, "100200345"),
            Make(ReportKind.Lost, DocumentType.StudentCard, 9, 9, "Carrefour Ndokoti", ReportStatus.Searching, 0, 2741),
            Make(ReportKind.Lost, DocumentType.DrivingLicense, 54, 8, "Marché Central", ReportStatus.Returned, 6, 1204, "LC8842109"),
            Make(ReportKind.Found, DocumentType.Diploma, 2, 15, "Ndokoti", ReportStatus.Returned, 0.8, 4698),
            Make(ReportKind.Found, DocumentType.Diploma, 6, 11, "Akwa, boulevard de la Liberté", ReportStatus.MatchFound, 1, 4102),
            Make(ReportKind.Found, DocumentType.TaxCard, 126, 9, "Bonabéri, gare routière", ReportStatus.Expired, ReportStatusRules.ExpiryDays, 931),
        }
        .OrderByDescending(r => r.CreatedAt)
        .ToList();
    }
}
