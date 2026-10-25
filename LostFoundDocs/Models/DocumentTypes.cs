namespace LostFoundDocs.Models;

public sealed record DocumentTypeInfo(
    DocumentType Type,
    string Label,
    bool IsFeminine,
    string NumberLabel,
    string IssuerLabel,
    string IssuerExample,
    bool NumberRecommended);

/// <summary>Libellés et règles par type de document (le numéro n'est jamais obligatoire).</summary>
public static class DocumentTypes
{
    public static readonly IReadOnlyList<DocumentTypeInfo> All = new List<DocumentTypeInfo>
    {
        new(DocumentType.NationalId, "Carte nationale d’identité", true, "Numéro de la CNI", "Autorité de délivrance", "Ex. Délégation régionale de la Sûreté nationale", true),
        new(DocumentType.StudentCard, "Carte d’étudiant", true, "Matricule", "Établissement", "Ex. Université de Douala", false),
        new(DocumentType.Passport, "Passeport", false, "Numéro du passeport", "Autorité de délivrance", "Ex. Sûreté nationale", true),
        new(DocumentType.DrivingLicense, "Permis de conduire", false, "Numéro du permis", "Autorité de délivrance", "Ex. Direction des transports terrestres", true),
        new(DocumentType.Diploma, "Diplôme", false, "Numéro du diplôme", "Établissement / organisme de délivrance", "Ex. Université de Douala", true),
        new(DocumentType.Certificate, "Certificat", false, "Numéro ou référence du certificat", "Organisme de délivrance", "Ex. Université de Douala", false),
        new(DocumentType.TaxCard, "Carte de contribuable", true, "Numéro d’identifiant unique (NIU)", "Administration émettrice", "Ex. Direction générale des impôts", true),
        new(DocumentType.ProfessionalCard, "Carte professionnelle", true, "Numéro de carte", "Employeur / organisme", "Ex. Ordre professionnel", false),
        new(DocumentType.ProfessionalLicense, "Licence professionnelle", true, "Numéro de licence", "Organisme de délivrance", "Ex. Ordre professionnel", false),
        new(DocumentType.Other, "Autre document officiel", false, "Numéro ou référence (si existe)", "Organisme (si connu)", "Ex. Mairie de Douala", false),
    };

    public static DocumentTypeInfo Get(DocumentType type) => All.First(t => t.Type == type);

    /// <summary>« Perdue / Perdu / Trouvée / Trouvé » selon le genre du document.</summary>
    public static string EventVerb(DocumentType type, ReportKind kind)
    {
        var feminine = Get(type).IsFeminine;
        return kind == ReportKind.Lost
            ? (feminine ? "Perdue" : "Perdu")
            : (feminine ? "Trouvée" : "Trouvé");
    }
}
