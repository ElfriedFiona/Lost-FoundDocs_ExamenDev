using LostFoundDocs.Models;
using LostFoundDocs.Services.Mock;
using Xunit;

namespace LostFoundDocs.Tests;

public class ReportServiceTests : TestBase
{
    [Fact]
    public async Task Submitting_the_same_draft_twice_creates_one_report()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var before = reports.Reports.Count;
        var draft = LostDiploma();

        var first = await reports.SubmitAsync(draft);
        var second = await reports.SubmitAsync(draft);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(before + 1, reports.Reports.Count);
        Assert.Equal(draft.SubmittedReportId, first.Id);
    }

    [Fact]
    public async Task Simultaneous_double_tap_creates_one_report()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var before = reports.Reports.Count;
        var draft = LostDiploma();

        var results = await Task.WhenAll(reports.SubmitAsync(draft), reports.SubmitAsync(draft));

        Assert.Equal(results[0].Id, results[1].Id);
        Assert.Equal(before + 1, reports.Reports.Count);
    }

    [Fact]
    public async Task Status_changes_on_the_same_report_without_duplicates()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var report = await reports.SubmitAsync(LostDiploma());
        var count = reports.Reports.Count;

        var updated = await reports.UpdateStatusAsync(report.Id, ReportStatus.MatchFound);

        Assert.Equal(report.Id, updated.Id);
        Assert.Equal(count, reports.Reports.Count);
        Assert.Equal(ReportStatus.MatchFound, updated.Status);
        Assert.True(updated.UpdatedAt >= updated.CreatedAt);
    }

    [Fact]
    public async Task Forbidden_transition_throws_and_keeps_the_status()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var report = await reports.SubmitAsync(LostDiploma());

        await Assert.ThrowsAsync<InvalidOperationException>(() => reports.UpdateStatusAsync(report.Id, ReportStatus.Returned));
        Assert.Equal(ReportStatus.Searching, report.Status);
    }

    [Fact]
    public async Task Cancel_works_only_when_the_lifecycle_allows_it()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var returned = reports.Reports.First(r => r.Status == ReportStatus.Returned);
        Assert.False(await reports.CancelAsync(returned.Id));

        var fresh = await reports.SubmitAsync(LostDiploma());
        Assert.True(await reports.CancelAsync(fresh.Id));
        Assert.Equal(ReportStatus.Cancelled, fresh.Status);
    }

    [Fact]
    public async Task Invalid_drafts_are_refused()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var future = LostDiploma();
        future.EventDate = Today.AddDays(1);
        await Assert.ThrowsAsync<ArgumentException>(() => reports.SubmitAsync(future));

        var foundWithoutNameOrNumber = new ReportDraft { Kind = ReportKind.Found, Type = DocumentType.Passport, EventDate = Today, City = "Douala", Place = "Akwa" };
        await Assert.ThrowsAsync<ArgumentException>(() => reports.SubmitAsync(foundWithoutNameOrNumber));
    }

    [Fact]
    public async Task Found_report_may_omit_the_number_when_the_name_is_known()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        var draft = new ReportDraft { Kind = ReportKind.Found, Type = DocumentType.Passport, HolderName = "Nom Visible", EventDate = Today, City = "Douala", Place = "Akwa" };
        var report = await reports.SubmitAsync(draft);
        Assert.Null(report.Number);
    }

    [Fact]
    public async Task Reset_restores_the_original_demo_data()
    {
        var reports = new MockReportService(await SignedInDemoAsync());
        await reports.SubmitAsync(LostDiploma());
        reports.Reset();
        Assert.Equal(6, reports.Reports.Count);
    }
}
