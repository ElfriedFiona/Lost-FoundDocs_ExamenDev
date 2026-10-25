using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LostFoundDocs.Api.Tests;

/// <summary>Base SQLite réelle en mémoire (le schéma est créé par EnsureCreated, comme au démarrage du serveur).</summary>
public sealed class ExpiryJobTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly AppDbContext _db;
    private readonly FakeTime _time = new(new DateTime(2026, 10, 22, 8, 0, 0, DateTimeKind.Utc));

    public ExpiryJobTests()
    {
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
    }

    private Report AddReport(int ageDays, ReportStatus status = ReportStatus.Searching)
    {
        var created = _time.GetUtcNow().UtcDateTime.AddDays(-ageDays);
        var r = new Report
        {
            Id = Guid.NewGuid(), Reference = $"LF-2026-{Random.Shared.Next(100000, 999999)}", UserId = Guid.NewGuid(), ClientRequestId = Guid.NewGuid(),
            Kind = ReportKind.Lost, Type = DocumentType.Diploma, HolderName = "Test", EventDate = created.Date, City = "Douala", Place = "Akwa",
            Status = status, CreatedAt = created, UpdatedAt = created,
        };
        _db.Reports.Add(r);
        _db.SaveChanges();
        return r;
    }

    [Fact]
    public async Task Old_searching_reports_expire_and_recent_ones_are_untouched()
    {
        var old = AddReport(ageDays: 91);
        var recent = AddReport(ageDays: 10);
        var matched = AddReport(ageDays: 120, ReportStatus.MatchFound);

        var (expired, _) = await ExpiryJob.RunAsync(_db, new NotificationService(_db, _time), _time);

        Assert.Equal(1, expired);
        Assert.Equal(ReportStatus.Expired, (await _db.Reports.FirstAsync(r => r.Id == old.Id)).Status);
        Assert.Equal(ReportStatus.Searching, (await _db.Reports.FirstAsync(r => r.Id == recent.Id)).Status);
        Assert.Equal(ReportStatus.MatchFound, (await _db.Reports.FirstAsync(r => r.Id == matched.Id)).Status);
        Assert.Equal(1, await _db.Notifications.CountAsync(n => n.ReportId == old.Id));
    }

    [Fact]
    public async Task Reminder_is_sent_once_after_seven_days()
    {
        var report = AddReport(ageDays: 8);
        var notifications = new NotificationService(_db, _time);

        var first = await ExpiryJob.RunAsync(_db, notifications, _time);
        var second = await ExpiryJob.RunAsync(_db, notifications, _time);

        Assert.Equal(1, first.Reminders);
        Assert.Equal(0, second.Reminders);
        Assert.Equal(1, await _db.Notifications.CountAsync(n => n.ReportId == report.Id && n.Kind == NotificationKind.Reminder));
    }

    [Fact]
    public async Task Technical_data_is_purged()
    {
        var now = _time.GetUtcNow().UtcDateTime;
        _db.OtpCodes.Add(new OtpCode { Id = Guid.NewGuid(), Phone = "677000001", CreatedAt = now.AddDays(-2), ExpiresAt = now.AddDays(-2) });
        _db.OtpCodes.Add(new OtpCode { Id = Guid.NewGuid(), Phone = "677000002", CreatedAt = now.AddMinutes(-1), ExpiresAt = now.AddMinutes(9) });
        await _db.SaveChangesAsync();

        await ExpiryJob.RunAsync(_db, new NotificationService(_db, _time), _time);

        Assert.Equal(1, await _db.OtpCodes.CountAsync());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private sealed class FakeTime(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
    }
}
