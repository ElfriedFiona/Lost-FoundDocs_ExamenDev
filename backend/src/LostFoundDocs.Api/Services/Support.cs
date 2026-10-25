using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;

namespace LostFoundDocs.Api.Services;

public static class Names
{
    /// <summary>« Aïcha Mbarga » → « Aïcha M. » : nom montré à l'autre partie (confidentialité).</summary>
    private static string TitleCaseIfUniform(string part) =>
        part == part.ToLowerInvariant() || part == part.ToUpperInvariant()
            ? char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()
            : part;

    public static string Short(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "Utilisateur";
        var first = TitleCaseIfUniform(parts[0]);
        if (parts.Length == 1) return first;
        return $"{first} {char.ToUpperInvariant(parts[^1][0])}.";
    }
}

public static class DocumentTitles
{
    public static string Label(DocumentType type) => type switch
    {
        DocumentType.NationalId => "Carte nationale d’identité",
        DocumentType.StudentCard => "Carte d’étudiant",
        DocumentType.Passport => "Passeport",
        DocumentType.DrivingLicense => "Permis de conduire",
        DocumentType.Diploma => "Diplôme",
        DocumentType.Certificate => "Certificat",
        DocumentType.TaxCard => "Carte de contribuable",
        DocumentType.ProfessionalCard => "Carte professionnelle",
        DocumentType.ProfessionalLicense => "Licence professionnelle",
        _ => "Autre document officiel",
    };

    public static string Of(Report r) =>
        r.Type == DocumentType.Other && !string.IsNullOrWhiteSpace(r.CustomTitle) ? r.CustomTitle! : Label(r.Type);
}

/// <summary>Historique des événements de sécurité (connexions refusées, changements de mot de passe…).</summary>
public sealed class AuditService(AppDbContext db, TimeProvider time)
{
    public void Record(Guid? userId, string action, string? detail = null, string? ip = null) =>
        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(), UserId = userId, Action = action, Detail = detail, Ip = ip, At = time.GetUtcNow().UtcDateTime,
        });
}
