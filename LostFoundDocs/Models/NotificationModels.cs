using LostFoundDocs.Services;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Models;

public enum NotificationKind { Match, Message, Restitution, Reminder }

public sealed class AppNotification : ObservableObject
{
    private bool _isRead;

    public Guid Id { get; init; } = Guid.NewGuid();
    public NotificationKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Detail { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? ReportId { get; init; }
    public Guid? ConversationId { get; init; }

    public bool IsRead { get => _isRead; set { if (SetProperty(ref _isRead, value)) OnPropertyChanged(nameof(IsUnread)); } }
    public bool IsUnread => !IsRead;
    public bool IsToday => CreatedAt.Date == Clock.Today;

    public string Category => Kind switch
    {
        NotificationKind.Match => "Correspondance",
        NotificationKind.Message => "Message",
        NotificationKind.Restitution => "Restitution",
        _ => "Rappel",
    };

    public string Header => $"{Category} · {DateFormat.AgoCapitalized(CreatedAt, Clock.Now)}";
    public bool HasDetail => !string.IsNullOrEmpty(Detail);
}
