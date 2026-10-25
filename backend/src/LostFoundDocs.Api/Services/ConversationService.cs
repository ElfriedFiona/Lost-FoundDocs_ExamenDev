using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostFoundDocs.Api.Services;

/// <summary>Messagerie liée à une correspondance : uniquement entre les deux participants, fermée à la restitution ou à la contestation.</summary>
public sealed class ConversationService(AppDbContext db, TimeProvider time, NotificationService notifications)
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    /// <summary>Crée la conversation d'une correspondance (sans enregistrer : l'appelant valide l'ensemble).</summary>
    public Conversation CreateForMatch(Match match, Report lost, Report found)
    {
        var now = Now;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(), MatchId = match.Id, OwnerUserId = lost.UserId, FinderUserId = found.UserId, CreatedAt = now,
            OwnerLastReadAt = now, FinderLastReadAt = now,
        };
        db.Conversations.Add(conversation);
        AddSystemMessage(conversation.Id, "Correspondance trouvée : le propriétaire et la personne qui a trouvé le document ont été notifiés. Échangez ici pour organiser la restitution.");
        return conversation;
    }

    public Message AddSystemMessage(Guid conversationId, string text)
    {
        var m = new Message { Id = Guid.NewGuid(), ConversationId = conversationId, IsSystem = true, Text = text, CreatedAt = Now };
        db.Messages.Add(m);
        return m;
    }

    public async Task<IReadOnlyList<ConversationDto>> ListAsync(Guid userId)
    {
        var conversations = await db.Conversations.Where(c => c.OwnerUserId == userId || c.FinderUserId == userId).ToListAsync();
        var result = new List<ConversationDto>();
        foreach (var c in conversations)
        {
            var match = await db.Matches.FirstAsync(m => m.Id == c.MatchId);
            var userIsOwner = c.OwnerUserId == userId;
            var mine = await db.Reports.FirstAsync(r => r.Id == (userIsOwner ? match.LostReportId : match.FoundReportId));
            var counterpartUserId = userIsOwner ? c.FinderUserId : c.OwnerUserId;
            var counterpart = await db.Users.FirstOrDefaultAsync(u => u.Id == counterpartUserId);
            var lastRead = userIsOwner ? c.OwnerLastReadAt : c.FinderLastReadAt;
            var unread = await db.Messages.CountAsync(m => m.ConversationId == c.Id && !m.IsSystem && m.SenderUserId != userId && m.CreatedAt > lastRead);
            var last = await db.Messages.Where(m => m.ConversationId == c.Id).OrderByDescending(m => m.CreatedAt).FirstOrDefaultAsync();

            result.Add(new ConversationDto(c.Id, mine.Id, match.Id, DocumentTitles.Of(mine), mine.Reference, userIsOwner,
                counterpart is null ? "Utilisateur" : Names.Short(counterpart.FullName), c.ClosedAt is not null, unread,
                last?.CreatedAt ?? c.CreatedAt, last is null ? null : Preview(last, userId)));
        }
        return result.OrderByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task<IReadOnlyList<MessageDto>> MessagesAsync(Guid userId, Guid conversationId, DateTime? after, int limit)
    {
        await RequireParticipant(userId, conversationId);
        limit = Math.Clamp(limit, 1, 500);
        var query = db.Messages.Where(m => m.ConversationId == conversationId);
        if (after is { } a) query = query.Where(m => m.CreatedAt > a);
        var items = await query.OrderBy(m => m.CreatedAt).Take(limit).ToListAsync();
        return items.Select(m => ToDto(m, userId)).ToList();
    }

    public async Task<MessageDto> SendAsync(Guid userId, Guid conversationId, SendMessageRequest req)
    {
        var conversation = await RequireParticipant(userId, conversationId);
        var text = (req.Text ?? "").Trim();
        var v = new Validator();
        if (text.Length == 0) v.Add("text", "Écrivez un message avant d’envoyer.");
        else if (text.Length > 1000) v.Add("text", "1000 caractères maximum.");
        v.ThrowIfInvalid();

        if (conversation.ClosedAt is not null)
            throw ApiException.Conflict("conversation_closed", "Cette conversation est clôturée.");

        // Idempotence : un renvoi du même message (réseau instable) ne crée pas de doublon.
        if (req.ClientMessageId != Guid.Empty)
        {
            var existing = await db.Messages.FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.ClientMessageId == req.ClientMessageId && m.SenderUserId == userId);
            if (existing is not null) return ToDto(existing, userId);
        }

        var now = Now;
        var message = new Message
        {
            Id = Guid.NewGuid(), ConversationId = conversationId, SenderUserId = userId,
            ClientMessageId = req.ClientMessageId == Guid.Empty ? null : req.ClientMessageId, Text = text, CreatedAt = now,
        };
        db.Messages.Add(message);

        // Une seule notification « message » non lue par conversation (évite le spam).
        var recipient = conversation.OwnerUserId == userId ? conversation.FinderUserId : conversation.OwnerUserId;
        var existingNotification = await db.Notifications.FirstOrDefaultAsync(n =>
            n.UserId == recipient && n.ConversationId == conversationId && n.Kind == NotificationKind.Message && n.ReadAt == null);
        if (existingNotification is null)
        {
            var match = await db.Matches.FirstAsync(m => m.Id == conversation.MatchId);
            var recipientReportId = conversation.OwnerUserId == recipient ? match.LostReportId : match.FoundReportId;
            notifications.Add(recipient, NotificationKind.Message, "Un utilisateur a répondu à votre message.",
                "Ouvrez la conversation pour lire le message.", recipientReportId, match.Id, conversationId);
        }
        else
        {
            existingNotification.CreatedAt = now;
        }

        await db.SaveChangesAsync();
        return ToDto(message, userId);
    }

    public async Task MarkReadAsync(Guid userId, Guid conversationId)
    {
        var c = await RequireParticipant(userId, conversationId);
        if (c.OwnerUserId == userId) c.OwnerLastReadAt = Now; else c.FinderLastReadAt = Now;
        await db.SaveChangesAsync();
    }

    private async Task<Conversation> RequireParticipant(Guid userId, Guid conversationId) =>
        await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId && (c.OwnerUserId == userId || c.FinderUserId == userId))
        ?? throw ApiException.NotFound("Conversation introuvable.");

    private static MessageDto ToDto(Message m, Guid viewer) => new(m.Id, m.Text, m.SenderUserId == viewer && !m.IsSystem, m.IsSystem, m.CreatedAt);

    private static string Preview(Message m, Guid viewer)
    {
        var text = m.Text.Length > 80 ? m.Text[..80] + "…" : m.Text;
        return m.IsSystem ? text : m.SenderUserId == viewer ? "Vous : " + text : text;
    }
}
