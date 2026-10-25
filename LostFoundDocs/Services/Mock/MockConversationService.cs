using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Mock;

/// <summary>SIMULATION (frontend uniquement) : messagerie locale. Aucun message ne quitte l'appareil ; la réponse automatique est fictive.</summary>
public sealed class MockConversationService : IConversationService
{
    private readonly DemoWorld _world;

    public MockConversationService(DemoWorld world)
    {
        _world = world;
        _world.Cleared += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;

    public IReadOnlyList<Conversation> Conversations
    {
        get { lock (_world.Gate) { return _world.Current.Conversations.OrderByDescending(c => c.LastAt).ToList(); } }
    }

    public int TotalUnread
    {
        get { lock (_world.Gate) { return _world.Current.Conversations.Sum(c => c.UnreadCount); } }
    }

    public Conversation? Get(Guid id)
    {
        lock (_world.Gate) { return _world.Current.Conversations.FirstOrDefault(c => c.Id == id); }
    }

    public Conversation? GetForReport(Guid reportId)
    {
        lock (_world.Gate) { return _world.Current.Conversations.FirstOrDefault(c => c.ReportId == reportId); }
    }

    public Task<Conversation> EnsureForMatchAsync(Report report, MatchResult match)
    {
        Conversation conversation;
        lock (_world.Gate)
        {
            var existing = _world.Current.Conversations.FirstOrDefault(c => c.ReportId == report.Id);
            if (existing is not null) return Task.FromResult(existing);

            conversation = new Conversation
            {
                ReportId = report.Id,
                DocumentTitle = report.Title,
                Reference = report.Reference,
                UserIsOwner = report.Kind == ReportKind.Lost,
                CounterpartName = match.CounterpartName,
            };
            conversation.Add(new ChatMessage
            {
                IsSystem = true,
                Text = report.Kind == ReportKind.Lost ? "Le trouveur a été notifié automatiquement." : "Le propriétaire a été notifié automatiquement.",
                SentAt = Clock.Now,
            });
            _world.Current.Conversations.Add(conversation);
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(conversation);
    }

    public void SetViewed(Guid id, bool viewed)
    {
        var c = Get(id);
        if (c is null) return;
        c.IsViewed = viewed;
        if (viewed) { c.UnreadCount = 0; Changed?.Invoke(this, EventArgs.Empty); }
    }

    public Task MarkReadAsync(Guid id)
    {
        var c = Get(id);
        if (c is not null) c.UnreadCount = 0;
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task<ChatMessage> SendAsync(Guid id, string text)
    {
        var c = Get(id) ?? throw new KeyNotFoundException("Conversation introuvable.");
        if (c.IsClosed) throw new InvalidOperationException("Cette conversation est clôturée.");
        var message = new ChatMessage { Text = text.Trim(), IsMine = true, SentAt = Clock.Now };
        await MainThread.InvokeOnMainThreadAsync(() => c.Add(message));
        Changed?.Invoke(this, EventArgs.Empty);

        if (DemoSettings.AutoReply) _ = SimulateReplyAsync(c);
        return message;
    }

    public async Task AddSystemMessageAsync(Guid id, string text)
    {
        var c = Get(id);
        if (c is null) return;
        await MainThread.InvokeOnMainThreadAsync(() => c.Add(new ChatMessage { IsSystem = true, Text = text, SentAt = Clock.Now }));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task CloseAsync(Guid id)
    {
        var c = Get(id);
        if (c is not null) c.IsClosed = true;
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    // SIMULATION : réponse automatique fictive, clairement marquée dans l'interface.
    private async Task SimulateReplyAsync(Conversation c)
    {
        await Task.Delay(1500);
        var text = c.UserIsOwner
            ? "D’accord, je suis disponible demain matin pour vous remettre le document."
            : "Merci beaucoup ! Je suis disponible demain pour récupérer mon document.";
        var reply = new ChatMessage { Text = text, IsSimulatedReply = true, SentAt = Clock.Now };
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            c.Add(reply);
            if (!c.IsViewed) c.UnreadCount++;
        });
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
