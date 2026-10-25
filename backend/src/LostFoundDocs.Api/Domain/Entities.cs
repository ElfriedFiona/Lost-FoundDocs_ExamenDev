namespace LostFoundDocs.Api.Domain;

public enum DocumentType
{
    NationalId, StudentCard, Passport, DrivingLicense, Diploma,
    Certificate, TaxCard, ProfessionalCard, ProfessionalLicense, Other,
}

public enum ReportKind { Lost, Found }

public enum ReportStatus { Searching, MatchFound, MatchConfirmed, MatchDisputed, Returned, Expired, Cancelled }

public enum MatchStatus { Active, Disputed, Confirmed, Returned, Closed }

public enum NotificationKind { Match, Message, Restitution, Reminder }

public enum OtpPurpose { PhoneVerification, PasswordReset }

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    /// <summary>9 chiffres sans préfixe pays (ex. 677124821).</summary>
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = "";
    public DateTime? PhoneVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class OtpCode
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = "";
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public class ResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

public class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccessTokenHash { get; set; } = "";
    public DateTime AccessExpiresAt { get; set; }
    public string RefreshTokenHash { get; set; } = "";
    public DateTime RefreshExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? UserAgent { get; set; }
}

public class Report
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = "";
    public Guid UserId { get; set; }
    /// <summary>Clé d'idempotence fournie par le client : un même brouillon ne crée qu'un signalement.</summary>
    public Guid ClientRequestId { get; set; }
    public ReportKind Kind { get; set; }
    public DocumentType Type { get; set; }
    public string? CustomTitle { get; set; }
    public string HolderName { get; set; } = "";
    public string HolderNameNormalized { get; set; } = "";
    /// <summary>Empreinte HMAC du numéro : le numéro complet n'est jamais stocké.</summary>
    public string? NumberHash { get; set; }
    public string? NumberLast4 { get; set; }
    /// <summary>Empreinte HMAC de la date de naissance : la date complète n'est jamais stockée.</summary>
    public string? BirthDateHash { get; set; }
    public int? BirthYear { get; set; }
    public string? Issuer { get; set; }
    public string IssuerNormalized { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string City { get; set; } = "";
    public string Place { get; set; } = "";
    public string? Notes { get; set; }
    public string? PhotoFile { get; set; }
    public string? PhotoContentType { get; set; }
    public ReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
}

public class ReportCounter
{
    public int Year { get; set; }
    public int Next { get; set; }
}

public class Match
{
    public Guid Id { get; set; }
    public Guid LostReportId { get; set; }
    public Guid FoundReportId { get; set; }
    public int Score { get; set; }
    public string CriteriaJson { get; set; } = "[]";
    public MatchStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? DisputedByUserId { get; set; }
    public string? DisputeReason { get; set; }
    public DateTime? DisputedAt { get; set; }
}

public class Conversation
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid FinderUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime OwnerLastReadAt { get; set; }
    public DateTime FinderLastReadAt { get; set; }
}

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    /// <summary>Null pour un message système.</summary>
    public Guid? SenderUserId { get; set; }
    public Guid? ClientMessageId { get; set; }
    public string Text { get; set; } = "";
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string? Detail { get; set; }
    public Guid? ReportId { get; set; }
    public Guid? MatchId { get; set; }
    public Guid? ConversationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class Restitution
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public string Place { get; set; } = "";
    public string District { get; set; } = "";
    public DateTime Slot { get; set; }
    public Guid ProposedByUserId { get; set; }
    public DateTime ProposedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? OwnerConfirmedAt { get; set; }
    public DateTime? FinderConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class Receipt
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public string Reference { get; set; } = "";
    /// <summary>Code public court (ex. K7QD-4M2X), utilisé pour vérifier l'authenticité du reçu.</summary>
    public string VerificationCode { get; set; } = "";
    public string DocumentTitle { get; set; } = "";
    public string City { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string FinderName { get; set; } = "";
    public int Score { get; set; }
    public DateTime ReturnedAt { get; set; }
}

public class AuditEntry
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = "";
    public string? Detail { get; set; }
    public string? Ip { get; set; }
    public DateTime At { get; set; }
}
