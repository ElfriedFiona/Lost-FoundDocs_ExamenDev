using LostFoundDocs.Models;
using LostFoundDocs.Services.Mock;
using LostFoundDocs.Services.Validation;
using Xunit;

namespace LostFoundDocs.Tests;

public class MatchRestitutionTests : TestBase
{
    private sealed record Env(MockReportService Reports, DemoWorld World, FakeConversations Conversations,
                              MockNotificationService Notifications, MockMatchService Match, MockRestitutionService Restitution);

    private static async Task<Env> CreateAsync()
    {
        var auth = await SignedInDemoAsync();
        var reports = new MockReportService(auth);
        var world = new DemoWorld(auth, reports);
        var conversations = new FakeConversations();
        var notifications = new MockNotificationService(world);
        return new Env(reports, world, conversations, notifications,
            new MockMatchService(reports, world, conversations, notifications),
            new MockRestitutionService(reports, auth, world, conversations, notifications));
    }

    [Fact]
    public async Task Search_is_idempotent_and_creates_one_conversation_and_one_notification()
    {
        var env = await CreateAsync();
        var report = await env.Reports.SubmitAsync(LostDiploma());

        var first = await env.Match.SearchAsync(report.Id);
        var second = await env.Match.SearchAsync(report.Id);

        Assert.True(first.Found);
        Assert.Same(first, second);
        Assert.Equal(1, env.Conversations.Created);
        Assert.Single(env.Notifications.Notifications, n => n.ReportId == report.Id);
        Assert.Equal(ReportStatus.MatchFound, (await env.Reports.GetAsync(report.Id))!.Status);
    }

    [Fact]
    public async Task Match_dates_are_chronologically_coherent()
    {
        var env = await CreateAsync();
        var lost = await env.Reports.SubmitAsync(LostDiploma());
        var outcome = await env.Match.SearchAsync(lost.Id);
        Assert.InRange(outcome.Match!.CounterpartDate, lost.EventDate, Today);
        Assert.InRange(outcome.Match.Score, 89, 94);

        var found = await env.Reports.SubmitAsync(new ReportDraft { Kind = ReportKind.Found, Type = DocumentType.Passport, HolderName = "Pass Port", EventDate = Today.AddDays(-1), City = "Douala", Place = "Akwa" });
        var foundOutcome = await env.Match.SearchAsync(found.Id);
        Assert.Equal("Aïssatou N.", foundOutcome.Match!.CounterpartName);
        Assert.True(foundOutcome.Match.CounterpartDate <= found.EventDate);
    }

    [Fact]
    public async Task No_match_for_unknown_combination_and_demo_mode_can_force_results()
    {
        var env = await CreateAsync();
        var report = await env.Reports.SubmitAsync(new ReportDraft { Kind = ReportKind.Lost, Type = DocumentType.Certificate, HolderName = "Test User", EventDate = Today, City = "Douala", Place = "Akwa" });
        Assert.False((await env.Match.SearchAsync(report.Id)).Found);
        Assert.Equal(0, env.Conversations.Created);

        DemoSettings.MatchMode = MatchMode.AlwaysMatch;
        Assert.True((await env.Match.SearchAsync(report.Id, force: true)).Found);

        DemoSettings.MatchMode = MatchMode.NeverMatch;
        var other = await env.Reports.SubmitAsync(LostDiploma("Bonanjo"));
        Assert.False((await env.Match.SearchAsync(other.Id)).Found);
    }

    [Fact]
    public async Task Reporting_an_error_disputes_the_match_without_creating_reports()
    {
        var env = await CreateAsync();
        var report = await env.Reports.SubmitAsync(LostDiploma());
        await env.Match.SearchAsync(report.Id);
        var count = env.Reports.Reports.Count;

        await env.Match.ReportErrorAsync(report.Id, "Ce n’est pas mon document");

        Assert.True(env.Match.GetMatch(report.Id)!.IsDisputed);
        Assert.Equal(ReportStatus.MatchDisputed, (await env.Reports.GetAsync(report.Id))!.Status);
        Assert.Equal(count, env.Reports.Reports.Count);
    }

    [Fact]
    public async Task Finder_flow_proposal_then_confirmation_produces_a_coherent_receipt()
    {
        var env = await CreateAsync();
        var found = await env.Reports.SubmitAsync(new ReportDraft { Kind = ReportKind.Found, Type = DocumentType.Passport, HolderName = "Pass Port", EventDate = Today.AddDays(-1), City = "Douala", Place = "Akwa" });
        var match = (await env.Match.SearchAsync(found.Id)).Match!;

        await Assert.ThrowsAsync<InvalidOperationException>(() => env.Restitution.ConfirmAsync(found.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => env.Restitution.ProposeAsync(found.Id, RestitutionRules.Places[0], Clock_Now().AddHours(-1)));

        var slot = RestitutionRules.Combine(RestitutionRules.Days(Today)[0].Date, "10:00");
        await env.Restitution.ProposeAsync(found.Id, RestitutionRules.Places[0], slot);
        Assert.Equal(ReportStatus.MatchConfirmed, (await env.Reports.GetAsync(found.Id))!.Status);

        var receipt = await env.Restitution.ConfirmAsync(found.Id);
        Assert.Equal(ReportStatus.Returned, (await env.Reports.GetAsync(found.Id))!.Status);
        Assert.Equal(found.Reference, receipt.Reference);
        Assert.Equal("Aïcha M.", receipt.HandedByName);
        Assert.Equal("Aïssatou N.", receipt.OwnerName);
        Assert.True(receipt.ReturnedAt >= match.FoundAt);
        Assert.True(env.Conversations.GetForReport(found.Id)!.IsClosed);
    }

    [Fact]
    public async Task Owner_flow_roles_are_reversed_and_everything_stays_consistent()
    {
        var env = await CreateAsync();
        var lost = await env.Reports.SubmitAsync(LostDiploma());
        var match = (await env.Match.SearchAsync(lost.Id)).Match!;

        var proposal = await env.Restitution.SimulateCounterpartProposalAsync(lost.Id);
        Assert.False(proposal.ProposedByUser);
        await env.Restitution.AcceptAsync(lost.Id);
        var receipt = await env.Restitution.ConfirmAsync(lost.Id);

        Assert.Equal("Aïcha M.", receipt.OwnerName);
        Assert.Equal(match.CounterpartName, receipt.HandedByName);

        var now = Clock_Now();
        var problems = DemoWorldValidator.Validate(env.World.Current, env.Reports.Reports, now)
            .Concat(DemoDataValidator.Validate(env.Reports.Reports, now));
        Assert.Empty(problems);
    }

    [Fact]
    public void Verification_code_is_deterministic()
    {
        Assert.Equal(Receipt.BuildCode("LF-2026-009391"), Receipt.BuildCode("LF-2026-009391"));
        Assert.StartsWith("9391-", Receipt.BuildCode("LF-2026-009391"));
    }

    private static DateTime Clock_Now() => LostFoundDocs.Services.Clock.Now;
}
