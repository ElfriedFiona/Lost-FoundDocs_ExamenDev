using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using LostFoundDocs.Api.Pdf;
using LostFoundDocs.Api.Qr;
using LostFoundDocs.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

/// <summary>Reçu de restitution : enregistré en base, vérifiable publiquement par son code (URL du QR code) et téléchargeable en PDF.</summary>
public sealed class ReceiptService(AppDbContext db, IOptions<PublicOptions> publicOptions, IOptions<RestitutionOptions> restitution, MatchQueries queries)
{
    private string VerificationUrl(string code) => $"{publicOptions.Value.BaseUrl.TrimEnd('/')}/verify/{code}";

    /// <summary>Crée le reçu d'une restitution terminée (sans enregistrer : l'appelant valide l'ensemble).</summary>
    public async Task<Receipt> CreateAsync(Match match, Report lost, Report found, User owner, User finder, DateTime now)
    {
        string code;
        do { code = Tokens.VerificationCode(); }
        while (await db.Receipts.AnyAsync(r => r.VerificationCode == code));

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(), MatchId = match.Id, Reference = lost.Reference, VerificationCode = code,
            DocumentTitle = DocumentTitles.Of(lost), City = lost.City, OwnerName = Names.Short(owner.FullName),
            FinderName = Names.Short(finder.FullName), Score = match.Score, ReturnedAt = now,
        };
        db.Receipts.Add(receipt);
        return receipt;
    }

    public ReceiptDto ToDto(Receipt r, Guid reportId) => new(
        r.MatchId, reportId, r.Reference, r.VerificationCode, r.DocumentTitle, r.City, r.OwnerName, r.FinderName, r.Score, r.ReturnedAt,
        VerificationUrl(r.VerificationCode), QrEncoder.ToRows(QrEncoder.Encode(VerificationUrl(r.VerificationCode))));

    public async Task<ReceiptDto> GetForParticipantAsync(Guid userId, Guid matchId)
    {
        var (match, mine, _) = await queries.LoadForParticipantAsync(userId, matchId);
        var receipt = await db.Receipts.FirstOrDefaultAsync(r => r.MatchId == match.Id) ?? throw ApiException.NotFound("Aucun reçu pour cette correspondance.");
        return ToDto(receipt, mine.Id);
    }

    public async Task<VerifyReceiptDto?> VerifyAsync(string code)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        var r = await db.Receipts.FirstOrDefaultAsync(x => x.VerificationCode == normalized);
        return r is null ? null : new VerifyReceiptDto(true, r.Reference, r.DocumentTitle, r.City, r.ReturnedAt, r.OwnerName, r.FinderName);
    }

    public async Task<byte[]> BuildPdfAsync(Guid userId, Guid matchId)
    {
        var (match, _, _) = await queries.LoadForParticipantAsync(userId, matchId);
        var r = await db.Receipts.FirstOrDefaultAsync(x => x.MatchId == match.Id) ?? throw ApiException.NotFound("Aucun reçu pour cette correspondance.");
        var url = VerificationUrl(r.VerificationCode);
        var qr = QrEncoder.Encode(url);
        var offset = restitution.Value.UtcOffsetHours;

        var pdf = new SimplePdf();
        pdf.Rectangle(0, 782, 595, 60, 0.08);
        pdf.Text(50, 805, 22, true, "Lost&Found Docs");
        pdf.Text(50, 745, 11, false, "Reçu numérique de restitution");
        pdf.Text(50, 715, 26, true, r.Reference);
        pdf.Text(50, 690, 12, true, "Restitution confirmée");
        pdf.Line(50, 676, 545, 676);

        var y = 650.0;
        void Row(string label, string value)
        {
            pdf.Text(50, y, 11, false, label);
            pdf.Text(220, y, 11, true, value);
            y -= 26;
        }
        Row("Document", r.DocumentTitle);
        Row("Date", Formatting.LocalDateTime(r.ReturnedAt, offset));
        Row("Ville", r.City);
        Row("Propriétaire", r.OwnerName);
        Row("Remis par", r.FinderName);
        Row("Correspondance", $"{r.Score} % - automatique");

        pdf.Line(50, y + 6, 545, y + 6);
        y -= 20;
        pdf.Text(50, y, 11, false, "Code de vérification");
        pdf.Text(50, y - 24, 20, true, r.VerificationCode);
        pdf.Text(50, y - 48, 9, false, "Scannez le QR code ou ouvrez l'adresse ci-dessous pour vérifier l'authenticité du reçu :");
        pdf.Text(50, y - 62, 9, false, url);

        const double cell = 4.2, quiet = 4;
        var size = qr.Length;
        var originX = 545 - (size + quiet * 2) * cell;
        var originY = y - 70 - size * cell;
        pdf.Rectangle(originX, originY - quiet * cell, (size + quiet * 2) * cell, (size + quiet * 2) * cell, 1);
        for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
                if (qr[row][col]) pdf.Rectangle(originX + (col + quiet) * cell, originY + (size - 1 - row + quiet) * cell - quiet * cell, cell, cell, 0);

        pdf.Text(50, 60, 9, false, "Ce reçu atteste la remise du document à son propriétaire via Lost&Found Docs.");
        pdf.Text(50, 46, 9, false, "Son authenticité est vérifiable en ligne avec le code ci-dessus.");
        return pdf.Build();
    }
}
