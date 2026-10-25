using LostFoundDocs.Api.Domain;

namespace LostFoundDocs.Api.Contracts;

// ---------- Authentification ----------
public sealed record RegisterRequest(string? FullName, string? Phone, string? Email, string? Password);
public sealed record LoginRequest(string? Phone, string? Password);
public sealed record RefreshRequest(string? RefreshToken);
public sealed record ForgotPasswordRequest(string? Phone);
public sealed record VerifyResetCodeRequest(string? Phone, string? Code);
public sealed record ResetPasswordRequest(string? ResetToken, string? NewPassword);
public sealed record VerifyPhoneRequest(string? Code);
public sealed record UpdateProfileRequest(string? FullName, string? Email);
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
public sealed record DeleteAccountRequest(string? Password);

public sealed record TokenDto(string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt);
public sealed record UserDto(Guid Id, string FullName, string Phone, string? Email, bool PhoneVerified, DateTime CreatedAt);
/// <summary>DevOtp n'est renseigné qu'en développement (Security:ExposeOtpInResponse).</summary>
public sealed record AuthResponse(UserDto User, TokenDto Tokens, string? DevOtp);
public sealed record OtpSentResponse(bool Sent, string? DevOtp);
public sealed record ResetTokenResponse(string ResetToken);

// ---------- Signalements ----------
public sealed record CreateReportRequest(
    Guid ClientRequestId, ReportKind Kind, DocumentType DocumentType, string? CustomTitle, string? HolderName,
    string? Number, DateOnly? BirthDate, string? Issuer, DateOnly? EventDate, string? City, string? Place, string? Notes);

public sealed record ReportDto(
    Guid Id, string Reference, ReportKind Kind, DocumentType DocumentType, string Title, string? CustomTitle, string HolderName,
    string? MaskedNumber, int? BirthYear, string? Issuer, DateOnly EventDate, string City, string Place, string? Notes,
    bool HasPhoto, ReportStatus Status, DateTime CreatedAt, DateTime UpdatedAt, Guid? MatchId);

// ---------- Correspondances ----------
public sealed record CriterionDto(string Name, string State);

public sealed record MatchDto(
    Guid Id, Guid ReportId, ReportKind UserSide, string CounterpartReference, string CounterpartName, DocumentType DocumentType,
    string City, DateOnly CounterpartDate, int Score, string Strength, IReadOnlyList<CriterionDto> Criteria, MatchStatus Status,
    bool IsDisputed, string? DisputeReason, DateTime FoundAt, Guid? ConversationId);

public sealed record SearchResultDto(bool Found, MatchDto? Match);
public sealed record DisputeRequest(string? Reason);

// ---------- Messagerie ----------
public sealed record ConversationDto(
    Guid Id, Guid ReportId, Guid MatchId, string DocumentTitle, string Reference, bool UserIsOwner, string CounterpartName,
    bool IsClosed, int UnreadCount, DateTime LastMessageAt, string? LastMessagePreview);

public sealed record MessageDto(Guid Id, string Text, bool IsMine, bool IsSystem, DateTime CreatedAt);
public sealed record SendMessageRequest(Guid ClientMessageId, string? Text);

// ---------- Notifications ----------
public sealed record NotificationDto(
    Guid Id, NotificationKind Kind, string Title, string? Detail, Guid? ReportId, Guid? MatchId, Guid? ConversationId,
    DateTime CreatedAt, bool IsRead);

// ---------- Restitution ----------
public sealed record MeetingPlaceDto(string Name, string District);
public sealed record ProposeRequest(string? PlaceName, DateTimeOffset? Slot);

public sealed record ProposalDto(string Place, string District, DateTime Slot, bool ProposedByMe, bool Accepted);

public sealed record ReceiptDto(
    Guid MatchId, Guid ReportId, string Reference, string VerificationCode, string DocumentTitle, string City, string OwnerName,
    string FinderName, int Score, DateTime ReturnedAt, string VerificationUrl, IReadOnlyList<string> QrRows);

public sealed record RestitutionDto(
    Guid MatchId, ProposalDto? Proposal, bool MyConfirmed, bool OtherConfirmed, bool Completed, ReceiptDto? Receipt);

public sealed record VerifyReceiptDto(
    bool Valid, string Reference, string DocumentTitle, string City, DateTime ReturnedAt, string OwnerName, string FinderName);
