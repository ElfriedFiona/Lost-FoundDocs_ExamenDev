using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using LostFoundDocs.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace LostFoundDocs.Api.Services;

public sealed class ReportService(AppDbContext db, TimeProvider time, Fingerprinter fingerprints, MatchingService matching, MatchQueries queries,
                                  NotificationService notifications, ConversationService conversations, PhotoStorage photos)
{
    private static readonly SemaphoreSlim ReferenceGate = new(1, 1);
    private const int MaxReportsPerDay = 20;
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    /// <summary>Crée un signalement. Idempotent : le même ClientRequestId renvoie le signalement déjà créé (Created = false).</summary>
    public async Task<(ReportDto Report, bool Created)> CreateAsync(Guid userId, CreateReportRequest req)
    {
        if (req.ClientRequestId == Guid.Empty)
            throw ApiException.BadRequest("validation_failed", "Certaines informations sont invalides.",
                new Dictionary<string, string[]> { ["clientRequestId"] = new[] { "Identifiant de requête obligatoire." } });

        var existing = await db.Reports.FirstOrDefaultAsync(r => r.UserId == userId && r.ClientRequestId == req.ClientRequestId);
        if (existing is not null) return (await ToDtoAsync(existing), false);

        var now = Now;
        Validate(req, now);

        var since = now.AddHours(-24);
        if (await db.Reports.CountAsync(r => r.UserId == userId && r.CreatedAt > since) >= MaxReportsPerDay)
            throw ApiException.TooMany("Limite de signalements atteinte pour aujourd’hui. Réessayez demain.");

        var number = string.IsNullOrWhiteSpace(req.Number) ? null : new string(req.Number.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var holder = (req.HolderName ?? "").Trim();
        var report = new Report
        {
            Id = Guid.NewGuid(), UserId = userId, ClientRequestId = req.ClientRequestId, Kind = req.Kind, Type = req.DocumentType,
            CustomTitle = req.DocumentType == DocumentType.Other ? req.CustomTitle?.Trim() : null,
            HolderName = holder, HolderNameNormalized = TextNormalizer.NormalizeName(holder),
            NumberHash = number is null ? null : fingerprints.Number(number), NumberLast4 = number is null ? null : number[^4..],
            BirthDateHash = req.BirthDate is { } b ? fingerprints.BirthDate(b.ToDateTime(TimeOnly.MinValue)) : null,
            BirthYear = req.BirthDate?.Year,
            Issuer = string.IsNullOrWhiteSpace(req.Issuer) ? null : req.Issuer.Trim(), IssuerNormalized = TextNormalizer.Normalize(req.Issuer),
            EventDate = req.EventDate!.Value.ToDateTime(TimeOnly.MinValue), City = req.City!.Trim(), Place = req.Place!.Trim(),
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            Status = ReportStatus.Searching, CreatedAt = now, UpdatedAt = now,
        };
        report.Reference = await NextReferenceAsync(now.Year);
        db.Reports.Add(report);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Deux requêtes simultanées avec le même ClientRequestId : la contrainte d'unicité a joué, on renvoie le signalement existant.
            db.ChangeTracker.Clear();
            var winner = await db.Reports.FirstOrDefaultAsync(r => r.UserId == userId && r.ClientRequestId == req.ClientRequestId);
            if (winner is null) throw;
            return (await ToDtoAsync(winner), false);
        }

        await matching.TryMatchAsync(report.Id);
        report = await db.Reports.FirstAsync(r => r.Id == report.Id);
        return (await ToDtoAsync(report), true);
    }

    private static void Validate(CreateReportRequest req, DateTime now)
    {
        var v = new Validator();
        if (!Enum.IsDefined(req.Kind)) v.Add("kind", "Type de signalement invalide.");
        if (!Enum.IsDefined(req.DocumentType)) v.Add("documentType", "Type de document invalide.");

        if (req.Kind == ReportKind.Lost) v.FullName("holderName", req.HolderName);
        else if (string.IsNullOrWhiteSpace(req.HolderName) && string.IsNullOrWhiteSpace(req.Number))
            v.Add("holderName", "Renseignez au moins le nom ou le numéro visible sur le document.");
        else if (!string.IsNullOrWhiteSpace(req.HolderName)) v.FullName("holderName", req.HolderName);

        if (req.DocumentType == DocumentType.Other && string.IsNullOrWhiteSpace(req.CustomTitle))
            v.Add("customTitle", "Indiquez l’intitulé du document.");
        else if (req.CustomTitle is { Length: > 120 }) v.Add("customTitle", "120 caractères maximum.");

        v.OptionalDocumentNumber("number", req.Number);
        if (req.Issuer is { Length: > 200 }) v.Add("issuer", "200 caractères maximum.");
        v.Required("city", req.City, "La ville est obligatoire.", 80);
        v.Required("place", req.Place, "Indiquez le lieu, même approximatif.", 200);
        v.Notes("notes", req.Notes);

        var today = now.Date;
        if (req.EventDate is null) v.Add("eventDate", req.Kind == ReportKind.Lost ? "La date de perte est obligatoire." : "La date de découverte est obligatoire.");
        else if (req.EventDate.Value.ToDateTime(TimeOnly.MinValue) > today.AddDays(1)) v.Add("eventDate", "La date ne peut pas être dans le futur.");
        else if (req.EventDate.Value.ToDateTime(TimeOnly.MinValue) < today.AddYears(-10)) v.Add("eventDate", "Date trop ancienne.");

        if (req.BirthDate is { } birth)
        {
            var bd = birth.ToDateTime(TimeOnly.MinValue);
            if (bd > today) v.Add("birthDate", "La date de naissance ne peut pas être dans le futur.");
            else if (bd < today.AddYears(-120)) v.Add("birthDate", "Date de naissance invalide.");
            else if (req.EventDate is { } ev && ev.ToDateTime(TimeOnly.MinValue) < bd) v.Add("eventDate", "La date ne peut pas précéder la date de naissance.");
        }
        v.ThrowIfInvalid();
    }

    private async Task<string> NextReferenceAsync(int year)
    {
        await ReferenceGate.WaitAsync();
        try
        {
            var counter = await db.ReportCounters.FirstOrDefaultAsync(c => c.Year == year);
            if (counter is null)
            {
                counter = new ReportCounter { Year = year, Next = 1 };
                db.ReportCounters.Add(counter);
            }
            var n = counter.Next++;
            await db.SaveChangesAsync();
            return $"LF-{year}-{n:D6}";
        }
        finally { ReferenceGate.Release(); }
    }

    public async Task<IReadOnlyList<ReportDto>> ListAsync(Guid userId, ReportKind? kind, ReportStatus? status)
    {
        var query = db.Reports.Where(r => r.UserId == userId);
        if (kind is { } k) query = query.Where(r => r.Kind == k);
        if (status is { } s) query = query.Where(r => r.Status == s);
        var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        var matches = await queries.CurrentMatchesAsync(reports);
        return reports.Select(r => ReportRules.ToDto(r, matches.TryGetValue(r.Id, out var m) ? m.Id : null)).ToList();
    }

    public async Task<ReportDto> GetAsync(Guid userId, Guid id) => await ToDtoAsync(await RequireOwned(userId, id));

    public async Task<ReportDto> CancelAsync(Guid userId, Guid id)
    {
        var report = await RequireOwned(userId, id);
        var now = Now;
        var match = await queries.CurrentMatchAsync(report);

        ReportRules.Transition(report, ReportStatus.Cancelled, now);

        // Annuler une fiche appariée libère l'autre fiche : elle repart en recherche.
        if (match is not null && match.Status is MatchStatus.Active or MatchStatus.Confirmed)
        {
            match.Status = MatchStatus.Closed;
            var otherId = match.LostReportId == report.Id ? match.FoundReportId : match.LostReportId;
            var other = await db.Reports.FirstAsync(r => r.Id == otherId);
            if (!ReportStatusRules.IsTerminal(other.Status) && other.Status != ReportStatus.Searching)
                ReportRules.Transition(other, ReportStatus.Searching, now);

            var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id);
            if (conversation is not null)
            {
                conversation.ClosedAt = now;
                conversations.AddSystemMessage(conversation.Id, "L’autre partie a annulé son signalement : la conversation est clôturée.");
            }
            notifications.Add(other.UserId, NotificationKind.Match, $"La correspondance pour « {DocumentTitles.Of(other)} » n’est plus valable.",
                "La recherche reprend automatiquement.", other.Id, match.Id, conversation?.Id);
            await db.SaveChangesAsync();
            await matching.TryMatchAsync(other.Id);
        }
        else
        {
            await db.SaveChangesAsync();
        }
        return await ToDtoAsync(await db.Reports.FirstAsync(r => r.Id == id));
    }

    /// <summary>« J'ai déjà récupéré mon document » : clôture d'une fiche dont la correspondance a été contestée.</summary>
    public async Task<ReportDto> ResolveRecoveredAsync(Guid userId, Guid id)
    {
        var report = await RequireOwned(userId, id);
        ReportRules.Transition(report, ReportStatus.Returned, Now);
        await db.SaveChangesAsync();
        return await ToDtoAsync(report);
    }

    public async Task SavePhotoAsync(Guid userId, Guid id, Stream content, CancellationToken ct)
    {
        var report = await RequireOwned(userId, id);
        var (file, contentType) = await photos.SaveAsync(report.Id, content, ct);
        report.PhotoFile = file;
        report.PhotoContentType = contentType;
        report.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);
    }

    public async Task<(Stream Stream, string ContentType)> OpenPhotoAsync(Guid userId, Guid id)
    {
        var report = await RequireOwned(userId, id);
        var stream = photos.Open(report.PhotoFile);
        if (stream is null || report.PhotoContentType is null) throw ApiException.NotFound("Aucune photo pour ce signalement.");
        return (stream, report.PhotoContentType);
    }

    private async Task<Report> RequireOwned(Guid userId, Guid id) =>
        await db.Reports.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId) ?? throw ApiException.NotFound("Signalement introuvable.");

    private async Task<ReportDto> ToDtoAsync(Report r)
    {
        var match = await queries.CurrentMatchAsync(r);
        return ReportRules.ToDto(r, match?.Id);
    }
}
