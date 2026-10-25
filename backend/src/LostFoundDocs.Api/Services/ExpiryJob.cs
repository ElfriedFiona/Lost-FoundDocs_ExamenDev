using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LostFoundDocs.Api.Services;

/// <summary>Tâche périodique : rappels à 7 jours, expiration à 90 jours des signalements sans correspondance, purge des données techniques.</summary>
public static class ExpiryJob
{
    public static async Task<(int Expired, int Reminders)> RunAsync(AppDbContext db, NotificationService notifications, TimeProvider time, CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime;

        var expiredLimit = now.AddDays(-ReportStatusRules.ExpiryDays);
        var toExpire = await db.Reports.Where(r => r.Status == ReportStatus.Searching && r.CreatedAt < expiredLimit).ToListAsync(ct);
        foreach (var r in toExpire)
        {
            ReportRules.Transition(r, ReportStatus.Expired, now);
            notifications.Add(r.UserId, NotificationKind.Reminder, $"Votre signalement « {DocumentTitles.Of(r)} » a expiré.",
                $"Aucune correspondance en {ReportStatusRules.ExpiryDays} jours. Vous pouvez refaire une déclaration.", r.Id);
        }

        // Enregistrer avant la requête suivante : sinon la base renverrait encore ces fiches comme « en recherche ».
        await db.SaveChangesAsync(ct);

        var reminderLimit = now.AddDays(-7);
        var toRemind = await db.Reports.Where(r => r.Status == ReportStatus.Searching && r.ReminderSentAt == null && r.CreatedAt < reminderLimit).ToListAsync(ct);
        foreach (var r in toRemind)
        {
            r.ReminderSentAt = now;
            notifications.Add(r.UserId, NotificationKind.Reminder, $"Votre signalement « {DocumentTitles.Of(r)} » reste actif.",
                "Nous comparons automatiquement chaque nouveau document déclaré.", r.Id);
        }

        // Purge des données techniques périmées
        var day = now.AddDays(-1);
        db.OtpCodes.RemoveRange(await db.OtpCodes.Where(o => o.CreatedAt < day).ToListAsync(ct));
        db.ResetTokens.RemoveRange(await db.ResetTokens.Where(t => t.ExpiresAt < day).ToListAsync(ct));
        var old = now.AddDays(-30);
        db.Sessions.RemoveRange(await db.Sessions.Where(s => s.RefreshExpiresAt < old || (s.RevokedAt != null && s.RevokedAt < old)).ToListAsync(ct));
        var year = now.AddDays(-365);
        db.AuditEntries.RemoveRange(await db.AuditEntries.Where(a => a.At < year).ToListAsync(ct));

        await db.SaveChangesAsync(ct);
        return (toExpire.Count, toRemind.Count);
    }
}

public sealed class ExpiryWorker(IServiceScopeFactory scopes, TimeProvider time, ILogger<ExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var (expired, reminders) = await ExpiryJob.RunAsync(
                        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                        scope.ServiceProvider.GetRequiredService<NotificationService>(), time, stoppingToken);
                    if (expired + reminders > 0) logger.LogInformation("Tâche périodique : {Expired} expirés, {Reminders} rappels.", expired, reminders);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Échec de la tâche périodique.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // arrêt normal du serveur
        }
    }
}
