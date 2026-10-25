using LostFoundDocs.Models;
using LostFoundDocs.Services;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using Xunit;

// Horloge et réglages de démonstration sont statiques : les tests ne s'exécutent donc pas en parallèle.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LostFoundDocs.Tests;

/// <summary>Horloge fixe qui avance d'une seconde à chaque lecture, et réglages de démonstration remis à zéro.</summary>
public abstract class TestBase : IDisposable
{
    private readonly Func<DateTime> _previousClock;
    private int _tick;

    protected TestBase()
    {
        _previousClock = Clock.Provider;
        Clock.Provider = () => new DateTime(2026, 10, 18, 12, 0, 0).AddSeconds(_tick++);
        DemoSettings.SimulatedLatencyMs = 0;
        DemoSettings.MatchMode = MatchMode.Auto;
        DemoSettings.AutoReply = true;
    }

    protected static DateTime Today => Clock.Today;

    public void Dispose() => Clock.Provider = _previousClock;

    protected static async Task<MockAuthService> SignedInDemoAsync()
    {
        var auth = new MockAuthService();
        await auth.SignInAsync(DemoSettings.DemoPhone, DemoSettings.DemoPassword);
        return auth;
    }

    protected static ReportDraft LostDiploma(string place = "Akwa") => new()
    {
        Kind = ReportKind.Lost, Type = DocumentType.Diploma, HolderName = "Aïcha Mbarga",
        EventDate = Clock.Today.AddDays(-2), City = "Douala", Place = place,
    };
}

/// <summary>Messagerie factice : compte les conversations créées (MockConversationService dépend de MAUI).</summary>
public sealed class FakeConversations : IConversationService
{
    private readonly List<Conversation> _list = new();
    public int Created { get; private set; }
    public IReadOnlyList<Conversation> Conversations => _list;
    public int TotalUnread => _list.Sum(c => c.UnreadCount);
    public event EventHandler? Changed { add { } remove { } }

    public Conversation? Get(Guid id) => _list.FirstOrDefault(c => c.Id == id);
    public Conversation? GetForReport(Guid reportId) => _list.FirstOrDefault(c => c.ReportId == reportId);

    public Task<Conversation> EnsureForMatchAsync(Report report, MatchResult match)
    {
        var existing = GetForReport(report.Id);
        if (existing is not null) return Task.FromResult(existing);
        var c = new Conversation
        {
            ReportId = report.Id, DocumentTitle = report.Title, Reference = report.Reference,
            UserIsOwner = report.Kind == ReportKind.Lost, CounterpartName = match.CounterpartName,
        };
        _list.Add(c);
        Created++;
        return Task.FromResult(c);
    }

    public void SetViewed(Guid id, bool viewed) { }
    public Task MarkReadAsync(Guid id) => Task.CompletedTask;
    public Task<ChatMessage> SendAsync(Guid id, string text) => Task.FromResult(new ChatMessage { Text = text, IsMine = true, SentAt = Clock.Now });

    public Task AddSystemMessageAsync(Guid id, string text)
    {
        Get(id)?.Add(new ChatMessage { IsSystem = true, Text = text, SentAt = Clock.Now });
        return Task.CompletedTask;
    }

    public Task CloseAsync(Guid id)
    {
        if (Get(id) is { } c) c.IsClosed = true;
        return Task.CompletedTask;
    }
}
