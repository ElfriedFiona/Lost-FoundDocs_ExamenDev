using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

/// <summary>
/// Organisation de la remise : proposition (lieu public + créneau), acceptation par l'autre partie, puis confirmation des DEUX parties.
/// La restitution n'est enregistrée (statut « Restitué », reçu) que lorsque le propriétaire et la personne qui a trouvé le document confirment.
/// </summary>
public sealed class RestitutionService(AppDbContext db, TimeProvider time, IOptions<RestitutionOptions> options, MatchQueries queries,
                                       NotificationService notifications, ConversationService conversations, ReceiptService receipts)
{
    private RestitutionOptions Options => options.Value;
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public IReadOnlyList<MeetingPlaceDto> Places() => Options.Places.Select(p => new MeetingPlaceDto(p.Name, p.District)).ToList();

    public async Task<RestitutionDto> GetAsync(Guid userId, Guid matchId)
    {
        var (match, mine, _) = await queries.LoadForParticipantAsync(userId, matchId);
        return await BuildAsync(userId, match, mine);
    }

    public async Task<RestitutionDto> ProposeAsync(Guid userId, Guid matchId, ProposeRequest req)
    {
        var (match, mine, other) = await queries.LoadForParticipantAsync(userId, matchId);
        RequireOpen(match, mine, other);

        var place = Options.Places.FirstOrDefault(p => string.Equals(p.Name, req.PlaceName?.Trim(), StringComparison.OrdinalIgnoreCase));
        var v = new Validator();
        if (place is null) v.Add("placeName", "Choisissez un lieu public parmi ceux proposés.");
        if (req.Slot is null) v.Add("slot", "Choisissez un jour et un créneau.");
        v.ThrowIfInvalid();

        var now = Now;
        var slot = req.Slot!.Value.UtcDateTime;
        var local = Formatting.ToLocal(slot, Options.UtcOffsetHours);
        var inHours = (local.Hour >= Options.FirstHour && local.Hour < Options.LastHour) || (local.Hour == Options.LastHour && local.Minute == 0);
        if (slot <= now.AddMinutes(30)) v.Add("slot", "Ce créneau est déjà passé ou trop proche.");
        else if (slot > now.AddDays(Options.MaxDaysAhead)) v.Add("slot", $"Le rendez-vous doit avoir lieu dans les {Options.MaxDaysAhead} prochains jours.");
        else if (!inHours) v.Add("slot", $"Choisissez un créneau entre {Options.FirstHour} h et {Options.LastHour} h (rendez-vous de jour).");
        v.ThrowIfInvalid();

        var restitution = await db.Restitutions.FirstOrDefaultAsync(r => r.MatchId == match.Id);
        if (restitution?.CompletedAt is not null) throw ApiException.Conflict("restitution_completed", "La restitution est déjà enregistrée.");
        if (restitution is null)
        {
            restitution = new Restitution { Id = Guid.NewGuid(), MatchId = match.Id };
            db.Restitutions.Add(restitution);
        }

        restitution.Place = place!.Name;
        restitution.District = place.District;
        restitution.Slot = slot;
        restitution.ProposedByUserId = userId;
        restitution.ProposedAt = now;
        restitution.AcceptedAt = null;
        restitution.OwnerConfirmedAt = null;
        restitution.FinderConfirmedAt = null;

        // Une nouvelle proposition annule un accord précédent.
        if (match.Status == MatchStatus.Confirmed)
        {
            match.Status = MatchStatus.Active;
            ReportRules.Transition(mine, ReportStatus.MatchFound, now);
            ReportRules.Transition(other, ReportStatus.MatchFound, now);
        }

        var me = await db.Users.FirstAsync(u => u.Id == userId);
        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id);
        var text = $"{Names.Short(me.FullName)} a proposé un rendez-vous : {place.Name} ({place.District}), le {Formatting.LongDate(local)} à {local:HH:mm}.";
        if (conversation is not null) conversations.AddSystemMessage(conversation.Id, text);
        notifications.Add(other.UserId, NotificationKind.Restitution, "Un rendez-vous de restitution vous est proposé.",
            $"{place.Name}, le {Formatting.LongDate(local)} à {local:HH:mm}.", other.Id, match.Id, conversation?.Id);
        await db.SaveChangesAsync();
        return await BuildAsync(userId, match, mine);
    }

    public async Task<RestitutionDto> AcceptAsync(Guid userId, Guid matchId)
    {
        var (match, mine, other) = await queries.LoadForParticipantAsync(userId, matchId);
        RequireOpen(match, mine, other);
        var restitution = await db.Restitutions.FirstOrDefaultAsync(r => r.MatchId == match.Id)
                          ?? throw ApiException.Conflict("no_proposal", "Aucune proposition à accepter.");
        if (restitution.ProposedByUserId == userId) throw ApiException.Conflict("own_proposal", "Vous ne pouvez pas accepter votre propre proposition.");
        if (restitution.AcceptedAt is not null) return await BuildAsync(userId, match, mine);

        var now = Now;
        restitution.AcceptedAt = now;
        match.Status = MatchStatus.Confirmed;
        ReportRules.Transition(mine, ReportStatus.MatchConfirmed, now);
        ReportRules.Transition(other, ReportStatus.MatchConfirmed, now);

        var me = await db.Users.FirstAsync(u => u.Id == userId);
        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id);
        if (conversation is not null) conversations.AddSystemMessage(conversation.Id, $"{Names.Short(me.FullName)} a accepté le rendez-vous.");
        notifications.Add(other.UserId, NotificationKind.Restitution, "Votre rendez-vous de restitution a été accepté.", restitution.Place,
            other.Id, match.Id, conversation?.Id);
        await db.SaveChangesAsync();
        return await BuildAsync(userId, match, mine);
    }

    public async Task<RestitutionDto> ConfirmAsync(Guid userId, Guid matchId)
    {
        var (match, mine, other) = await queries.LoadForParticipantAsync(userId, matchId);
        var restitution = await db.Restitutions.FirstOrDefaultAsync(r => r.MatchId == match.Id);
        if (restitution?.CompletedAt is not null) return await BuildAsync(userId, match, mine);
        RequireOpen(match, mine, other);
        if (restitution is null || restitution.AcceptedAt is null)
            throw ApiException.Conflict("not_accepted", "Le rendez-vous doit d’abord être accepté par les deux parties.");

        var now = Now;
        var userIsOwner = mine.Kind == ReportKind.Lost;
        if (userIsOwner) restitution.OwnerConfirmedAt ??= now; else restitution.FinderConfirmedAt ??= now;

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id);
        if (restitution.OwnerConfirmedAt is null || restitution.FinderConfirmedAt is null)
        {
            var me = await db.Users.FirstAsync(u => u.Id == userId);
            if (conversation is not null)
                conversations.AddSystemMessage(conversation.Id, $"{Names.Short(me.FullName)} a confirmé la remise du document. En attente de la confirmation de l’autre partie.");
            notifications.Add(other.UserId, NotificationKind.Restitution, "L’autre partie a confirmé la restitution.",
                "Confirmez à votre tour pour enregistrer la restitution.", other.Id, match.Id, conversation?.Id);
            await db.SaveChangesAsync();
            return await BuildAsync(userId, match, mine);
        }

        // Les deux parties ont confirmé : restitution enregistrée.
        var lost = userIsOwner ? mine : other;
        var found = userIsOwner ? other : mine;
        var owner = await db.Users.FirstAsync(u => u.Id == lost.UserId);
        var finder = await db.Users.FirstAsync(u => u.Id == found.UserId);

        restitution.CompletedAt = now;
        match.Status = MatchStatus.Returned;
        ReportRules.Transition(lost, ReportStatus.Returned, now);
        ReportRules.Transition(found, ReportStatus.Returned, now);
        await receipts.CreateAsync(match, lost, found, owner, finder, now);

        if (conversation is not null)
        {
            conversation.ClosedAt = now;
            conversations.AddSystemMessage(conversation.Id, $"Restitution confirmée · Réf. {lost.Reference}");
        }
        notifications.Add(lost.UserId, NotificationKind.Restitution,
            $"La restitution de votre {DocumentTitles.Of(lost).ToLowerInvariant()} a été enregistrée.", $"Référence : {lost.Reference}", lost.Id, match.Id, conversation?.Id);
        notifications.Add(found.UserId, NotificationKind.Restitution,
            $"La restitution du document « {DocumentTitles.Of(found)} » a été enregistrée.", $"Référence : {lost.Reference}", found.Id, match.Id, conversation?.Id);
        await db.SaveChangesAsync();
        return await BuildAsync(userId, match, mine);
    }

    private static void RequireOpen(Match match, Report mine, Report other)
    {
        if (match.Status is not (MatchStatus.Active or MatchStatus.Confirmed))
            throw ApiException.Conflict("match_closed", "Cette correspondance n’est plus active.");
        if (mine.Status is not (ReportStatus.MatchFound or ReportStatus.MatchConfirmed) ||
            other.Status is not (ReportStatus.MatchFound or ReportStatus.MatchConfirmed))
            throw ApiException.Conflict("match_closed", "Cette correspondance n’est plus active.");
    }

    private async Task<RestitutionDto> BuildAsync(Guid userId, Match match, Report mine)
    {
        var restitution = await db.Restitutions.FirstOrDefaultAsync(r => r.MatchId == match.Id);
        var receipt = await db.Receipts.FirstOrDefaultAsync(r => r.MatchId == match.Id);
        var userIsOwner = mine.Kind == ReportKind.Lost;

        ProposalDto? proposal = restitution is null || string.IsNullOrEmpty(restitution.Place)
            ? null
            : new ProposalDto(restitution.Place, restitution.District, restitution.Slot, restitution.ProposedByUserId == userId, restitution.AcceptedAt is not null);
        var myConfirmed = restitution is not null && (userIsOwner ? restitution.OwnerConfirmedAt : restitution.FinderConfirmedAt) is not null;
        var otherConfirmed = restitution is not null && (userIsOwner ? restitution.FinderConfirmedAt : restitution.OwnerConfirmedAt) is not null;
        return new RestitutionDto(match.Id, proposal, myConfirmed, otherConfirmed, restitution?.CompletedAt is not null,
            receipt is null ? null : receipts.ToDto(receipt, mine.Id));
    }
}
