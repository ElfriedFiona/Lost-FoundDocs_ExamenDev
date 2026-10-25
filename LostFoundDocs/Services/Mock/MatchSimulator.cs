using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// SIMULATION (frontend uniquement) : règle de démonstration de la recherche de correspondance.
/// Mode Auto : correspondance si (type, ville) figure dans le jeu fictif de l'autre camp ;
/// mode « Toujours » / « Jamais » : forcé depuis À propos > Mode démonstration.
/// Le futur moteur de correspondance sera côté serveur.
/// </summary>
public static class MatchSimulator
{
    // Documents « trouvés » fictifs, disponibles pour ceux qui déclarent une perte.
    private static readonly (DocumentType Type, string City, string Finder)[] FoundPool =
    {
        (DocumentType.Diploma, "Douala", "Brice T."),
        (DocumentType.NationalId, "Douala", "Yannick K."),
        (DocumentType.DrivingLicense, "Douala", "Paul M."),
    };

    // Signalements de perte fictifs, disponibles pour ceux qui déclarent une trouvaille.
    private static readonly (DocumentType Type, string City, string Owner)[] LostPool =
    {
        (DocumentType.Passport, "Douala", "Aïssatou N."),
        (DocumentType.StudentCard, "Douala", "Estelle B."),
        (DocumentType.TaxCard, "Douala", "Chantal O."),
    };

    public static (bool Found, string Counterpart) Evaluate(Report report, MatchMode mode)
    {
        if (mode == MatchMode.NeverMatch) return (false, string.Empty);

        var fallback = report.Kind == ReportKind.Lost ? "Yannick K." : "Aïssatou N.";
        string? name = report.Kind == ReportKind.Lost
            ? FoundPool.FirstOrDefault(p => p.Type == report.Type && p.City == report.City).Finder
            : LostPool.FirstOrDefault(p => p.Type == report.Type && p.City == report.City).Owner;

        if (!string.IsNullOrEmpty(name)) return (true, name);
        return mode == MatchMode.AlwaysMatch ? (true, fallback) : (false, string.Empty);
    }

    public static int ScoreFor(string reference) => 89 + reference.Sum(c => c) % 6;   // 89 à 94

    public static string CounterpartReference(string reference, int year) =>
        $"LF-{year}-{900000 + Math.Abs(reference.Aggregate(7, (h, c) => unchecked(h * 31 + c))) % 99999:D6}";

    /// <summary>Date de l'autre déclaration, toujours cohérente : découverte ≥ perte, et jamais dans le futur.</summary>
    public static DateTime CounterpartDate(Report report, DateTime today) =>
        report.Kind == ReportKind.Lost
            ? report.EventDate.Date.AddDays(Math.Min(1, (today.Date - report.EventDate.Date).Days))
            : report.EventDate.Date.AddDays(-1);

    public static IReadOnlyList<MatchCriterion> BuildCriteria(Report r) => new List<MatchCriterion>
    {
        new("Type de document", MatchState.Match),
        new("Nom", string.IsNullOrWhiteSpace(r.HolderName) ? MatchState.NotCompared : MatchState.Match),
        new("Numéro", string.IsNullOrWhiteSpace(r.Number) ? MatchState.NotCompared : MatchState.Match),
        new("Date de naissance", r.BirthDate is null ? MatchState.NotCompared : MatchState.Match),
        new("Établissement", string.IsNullOrWhiteSpace(r.Issuer) ? MatchState.NotCompared : MatchState.Partial),
        new("Ville", MatchState.Match),
        new("Date", MatchState.Match),
    };
}
