using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Abstractions;

public interface IConversationService
{
    IReadOnlyList<Conversation> Conversations { get; }
    int TotalUnread { get; }
    event EventHandler? Changed;

    Conversation? Get(Guid id);
    Conversation? GetForReport(Guid reportId);
    Task<Conversation> EnsureForMatchAsync(Report report, MatchResult match);

    void SetViewed(Guid id, bool viewed);
    Task MarkReadAsync(Guid id);
    Task<ChatMessage> SendAsync(Guid id, string text);
    Task AddSystemMessageAsync(Guid id, string text);
    Task CloseAsync(Guid id);
}
