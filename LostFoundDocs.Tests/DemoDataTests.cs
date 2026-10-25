using LostFoundDocs.Models;
using LostFoundDocs.Services.Mock;
using Xunit;

namespace LostFoundDocs.Tests;

public class DemoDataTests : TestBase
{
    [Fact]
    public async Task Demo_reports_match_the_mockup_lists_and_are_chronologically_consistent()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var list = reports.Reports;

        Assert.Equal(6, list.Count);
        Assert.Equal(3, list.Count(r => r.Kind == ReportKind.Lost));
        Assert.Equal(3, list.Count(r => r.Kind == ReportKind.Found));
        Assert.Equal(list.Count, list.Select(r => r.Reference).Distinct().Count());
        Assert.Empty(DemoDataValidator.Validate(list, Clock_Now()));
    }

    [Fact]
    public async Task Dates_are_relative_to_today_never_fixed()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var cni = reports.Reports.First(r => r.Type == DocumentType.NationalId);
        Assert.Equal(Today.AddDays(-5), cni.EventDate);
    }

    [Fact]
    public async Task Demo_world_is_consistent()
    {
        var auth = await SignedInDemoAsync();
        var reports = new MockReportService(auth);
        var world = new DemoWorld(auth, reports);
        var w = world.Current;

        Assert.Equal(3, w.Conversations.Count);
        Assert.Equal(1, w.Conversations.Sum(c => c.UnreadCount));
        Assert.Equal(5, w.Notifications.Count);
        Assert.Equal(3, w.Notifications.Count(n => !n.IsRead));
        Assert.Equal(2, w.Receipts.Count);
        Assert.Empty(DemoWorldValidator.Validate(w, reports.Reports, Clock_Now()));
    }

    [Fact]
    public async Task Validator_detects_chronological_errors()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var now = Clock_Now();
        var broken = new Report
        {
            UserId = Guid.NewGuid(), Reference = "LF-2026-999999", Kind = ReportKind.Found, Type = DocumentType.Diploma,
            EventDate = now.Date.AddDays(3), City = "Douala", Place = "x",
            CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-2), Status = ReportStatus.Returned,
        };
        var problems = DemoDataValidator.Validate(reports.Reports.Concat(new[] { broken }), now);
        Assert.Contains(problems, p => p.Contains("futur"));
        Assert.Contains(problems, p => p.Contains("avant"));
    }

    [Fact]
    public async Task New_account_starts_with_no_data()
    {
        var auth = new MockAuthService();
        await auth.RegisterAsync(new LostFoundDocs.Services.Abstractions.RegistrationRequest("Awa Nkou", "655443322", null, "Motdepasse1"));
        var reports = new MockReportService(auth);
        var world = new DemoWorld(auth, reports);
        Assert.Empty(reports.Reports);
        Assert.Empty(world.Current.Conversations);
        Assert.Empty(world.Current.Notifications);
    }

    private static DateTime Clock_Now() => LostFoundDocs.Services.Clock.Now;
}
