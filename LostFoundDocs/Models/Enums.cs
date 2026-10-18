namespace LostFoundDocs.Models;

public enum DocumentType
{
    NationalId, StudentCard, Passport, DrivingLicense, Diploma,
    Certificate, TaxCard, ProfessionalCard, ProfessionalLicense, Other,
}

public enum ReportKind { Lost, Found }

/// <summary>Cycle de vie d'un signalement. Les transitions autorisées sont dans <see cref="ReportStatusRules"/>.</summary>
public enum ReportStatus
{
    Searching,        // Recherche en cours
    MatchFound,       // Correspondance trouvée
    MatchConfirmed,   // Correspondance confirmée (échange engagé)
    MatchDisputed,    // Correspondance contestée (erreur signalée)
    Returned,         // Restitué
    Expired,          // Expiré / Archivé
    Cancelled,        // Annulé par l'utilisateur
}
