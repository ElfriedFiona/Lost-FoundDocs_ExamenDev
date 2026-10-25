using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Abstractions;

public interface INotificationService
{
    IReadOnlyList<AppNotification> Notifications { get; }
    int UnreadCount { get; }
    event EventHandler? Changed;

    Task AddAsync(NotificationKind kind, string title, string? detail, Guid? reportId, Guid? conversationId);
    Task MarkReadAsync(Guid id);
    Task MarkAllReadAsync();
}
