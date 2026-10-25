using LostFoundDocs.Models;

namespace LostFoundDocs.UI;

/// <summary>Correspondances modèle → présentation (icône, couleur de pastille), pour garder les modèles sans dépendance visuelle.</summary>
public static class Presentation
{
    public static string Icon(DocumentType type) => type switch
    {
        DocumentType.NationalId => Icons.IdCard,
        DocumentType.StudentCard => Icons.GraduationCap,
        DocumentType.Passport => Icons.BookUser,
        DocumentType.DrivingLicense => Icons.Car,
        DocumentType.Diploma => Icons.Award,
        DocumentType.Certificate => Icons.FileBadge,
        DocumentType.TaxCard => Icons.Landmark,
        DocumentType.ProfessionalCard => Icons.Briefcase,
        DocumentType.ProfessionalLicense => Icons.ScrollText,
        _ => Icons.FileText,
    };

    public static BadgeKind Kind(ReportStatus status) => status switch
    {
        ReportStatus.Searching => BadgeKind.Warning,
        ReportStatus.MatchFound => BadgeKind.Success,
        ReportStatus.MatchConfirmed => BadgeKind.Success,
        ReportStatus.MatchDisputed => BadgeKind.Warning,
        ReportStatus.Returned => BadgeKind.Success,
        _ => BadgeKind.Neutral,
    };
}
