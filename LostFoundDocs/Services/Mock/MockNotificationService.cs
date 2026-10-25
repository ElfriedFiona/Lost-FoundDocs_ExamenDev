using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Mock;

/// <summary>SIMULATION (frontend uniquement) : notifications générées localement ; aucune notification push réelle.</summary>
public sealed class MockNotificationService : INotificationService
{
    private readonly DemoWorld _world;

    public MockNotificationService(DemoWorld world)
    {
        _world = world;
        _world.Cleared += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;

    public IReadOnlyList<AppNotification> Notifications
    {
        get { lock (_world.Gate) { return _world.Current.Notifications.OrderByDescending(n => n.CreatedAt).ToList(); } }
    }

    public int UnreadCount
    {
        get { lock (_world.Gate) { return _world.Current.Notifications.Count(n => !n.IsRead); } }
    }

    public Task AddAsync(NotificationKind kind, string title, string? detail, Guid? reportId, Guid? conversationId)
    {
        lock (_world.Gate)
        {
            _world.Current.Notifications.Insert(0, new AppNotification
            {
                Kind = kind, Title = title, Detail = detail, CreatedAt = Clock.Now,
                ReportId = reportId, ConversationId = conversationId,
            });
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task MarkReadAsync(Guid id)
    {
        lock (_world.Gate)
        {
            var n = _world.Current.Notifications.FirstOrDefault(x => x.Id == id);
            if (n is not null) n.IsRead = true;
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task MarkAllReadAsync()
    {
        lock (_world.Gate)
        {
            foreach (var n in _world.Current.Notifications) n.IsRead = true;
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
