using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostFoundDocs.Api.Services;

/// <summary>Notifications intégrées à l'application (consultées par le client). Aucune notification push tierce.</summary>
public sealed class NotificationService(AppDbContext db, TimeProvider time)
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    /// <summary>Ajoute une notification SANS enregistrer : l'appelant valide l'ensemble de ses changements d'un seul SaveChanges.</summary>
    public Notification Add(Guid userId, NotificationKind kind, string title, string? detail, Guid? reportId = null, Guid? matchId = null,
                            Guid? conversationId = null)
    {
        var n = new Notification
        {
            Id = Guid.NewGuid(), UserId = userId, Kind = kind, Title = title, Detail = detail, ReportId = reportId, MatchId = matchId,
            ConversationId = conversationId, CreatedAt = Now,
        };
        db.Notifications.Add(n);
        return n;
    }

    public static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Kind, n.Title, n.Detail, n.ReportId, n.MatchId, n.ConversationId, n.CreatedAt, n.ReadAt is not null);

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, DateTime? since, int limit)
    {
        limit = Math.Clamp(limit, 1, 200);
        var query = db.Notifications.Where(n => n.UserId == userId);
        if (since is { } s) query = query.Where(n => n.CreatedAt > s);
        var items = await query.OrderByDescending(n => n.CreatedAt).Take(limit).ToListAsync();
        return items.Select(ToDto).ToList();
    }

    public Task<int> UnreadCountAsync(Guid userId) => db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null);

    public async Task MarkReadAsync(Guid userId, Guid id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId)
                ?? throw ApiException.NotFound("Notification introuvable.");
        n.ReadAt ??= Now;
        await db.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var now = Now;
        var unread = await db.Notifications.Where(n => n.UserId == userId && n.ReadAt == null).ToListAsync();
        foreach (var n in unread) n.ReadAt = now;
        await db.SaveChangesAsync();
    }
}
