using LostFoundDocs.Models;
namespace LostFoundDocs.Services.Abstractions;

public interface IMatchService
{
    Task<MatchResult?> SearchAsync(Guid reportId);
    Task<MatchResult?> GetAsync(Guid reportId);
    Task DisputeAsync(Guid reportId, string reason);
}
public interface IConversationService
{
    IReadOnlyList<Conversation> Conversations { get; }
    Task<Conversation> OpenAsync(Guid reportId);
    Task SendAsync(Guid reportId, string text);
    Task AddSystemAsync(Guid reportId, string text);
}
public interface INotificationService
{
    IReadOnlyList<AppNotification> Notifications { get; }
    void Add(Report report, string title, string route);
    void MarkAllRead();
}
public interface IRestitutionService
{
    Task<RestitutionProposal?> GetProposalAsync(Guid reportId);
    Task ProposeAsync(Guid reportId, string place, DateTime appointment);
    Task AcceptAsync(Guid reportId);
    Task<Receipt> ConfirmAsync(Guid reportId);
    Task<Receipt> GetReceiptAsync(Guid reportId);
}
