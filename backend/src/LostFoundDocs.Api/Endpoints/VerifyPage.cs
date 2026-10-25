using System.Net;
using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Services;

namespace LostFoundDocs.Api.Endpoints;

/// <summary>Page publique ouverte par le QR code d'un reçu : indique si le reçu est authentique.</summary>
public static class VerifyPage
{
    public static string Render(VerifyReceiptDto? r, int offsetHours)
    {
        string Row(string label, string value) => $"<tr><th>{WebUtility.HtmlEncode(label)}</th><td>{WebUtility.HtmlEncode(value)}</td></tr>";
        var body = r is null
            ? "<div class=\"ko\"><h1>Reçu introuvable</h1><p>Ce code ne correspond à aucun reçu émis par Lost&amp;Found Docs. Vérifiez le code ou méfiez-vous de ce document.</p></div>"
            : "<div class=\"ok\"><h1>Reçu authentique</h1><p>Ce reçu a bien été émis par Lost&amp;Found Docs.</p></div><table>"
              + Row("Référence", r.Reference) + Row("Document", r.DocumentTitle) + Row("Date", Formatting.LocalDateTime(r.ReturnedAt, offsetHours))
              + Row("Ville", r.City) + Row("Propriétaire", r.OwnerName) + Row("Remis par", r.FinderName) + "</table>";

        return $$"""
            <!doctype html><html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>Vérification d'un reçu - Lost&amp;Found Docs</title>
            <style>body{font-family:system-ui,sans-serif;background:#f8fafc;color:#17202a;margin:0;padding:24px}main{max-width:520px;margin:auto}
            .ok,.ko{border-radius:16px;padding:20px;margin-bottom:16px}.ok{background:#e8f8ee;border:1px solid #bdebd0}.ko{background:#fee2e2;border:1px solid #fbc4c4}
            h1{margin:0 0 8px;font-size:22px}table{width:100%;background:#fff;border:1px solid #e5e7eb;border-radius:16px;border-collapse:collapse}
            th,td{text-align:left;padding:12px 16px;border-bottom:1px solid #e5e7eb}th{color:#6b7280;font-weight:600;width:40%}footer{color:#6b7280;font-size:13px;margin-top:16px}</style></head>
            <body><main><p><strong>Lost&amp;Found Docs</strong></p>{{body}}<footer>Vérification publique : aucune donnée sensible n'est affichée.</footer></main></body></html>
            """;
    }
}
