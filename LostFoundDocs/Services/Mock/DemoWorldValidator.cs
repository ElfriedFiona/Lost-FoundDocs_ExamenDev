using System.Diagnostics;
using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Mock;

/// <summary>Cohérence chronologique des correspondances, conversations, notifications et reçus fictifs.</summary>
public static class DemoWorldValidator
{
    public static IReadOnlyList<string> Validate(UserWorld world, IReadOnlyList<Report> reports, DateTime now)
    {
        var problems = new List<string>();
        Report? Rep(Guid id) => reports.FirstOrDefault(r => r.Id == id);

        foreach (var m in world.Matches.Values)
        {
            var r = Rep(m.ReportId);
            if (r is null) { problems.Add($"correspondance {m.CounterpartReference} sans signalement"); continue; }
            if (m.FoundAt < r.CreatedAt) problems.Add($"[{r.Reference}] correspondance antérieure à la déclaration");
            if (m.FoundAt > now) problems.Add($"[{r.Reference}] correspondance dans le futur");
            if (r.Kind == ReportKind.Lost && m.CounterpartDate < r.EventDate.Date) problems.Add($"[{r.Reference}] trouvé avant la perte");
            if (r.Kind == ReportKind.Found && m.CounterpartDate > r.EventDate.Date) problems.Add($"[{r.Reference}] perte déclarée après la découverte");
            if (m.CounterpartDate.Date > now.Date) problems.Add($"[{r.Reference}] date de l'autre déclaration dans le futur");
            if (m.Score is < 0 or > 100) problems.Add($"[{r.Reference}] score hors limites");
        }

        foreach (var c in world.Conversations)
        {
            var r = Rep(c.ReportId);
            if (r is null) { problems.Add($"conversation {c.Reference} sans signalement"); continue; }
            var match = world.Matches.GetValueOrDefault(c.ReportId);
            DateTime? previous = null;
            foreach (var msg in c.Messages)
            {
                if (previous is not null && msg.SentAt < previous) problems.Add($"[{c.Reference}] messages dans le désordre");
                if (match is not null && msg.SentAt < match.FoundAt) problems.Add($"[{c.Reference}] message avant la correspondance");
                if (msg.SentAt > now) problems.Add($"[{c.Reference}] message dans le futur");
                previous = msg.SentAt;
            }
            if (c.UnreadCount > c.Messages.Count(x => !x.IsMine && !x.IsSystem)) problems.Add($"[{c.Reference}] trop de non lus");
            if (r.Status == ReportStatus.Returned && world.Receipts.ContainsKey(c.ReportId) && !c.IsClosed) problems.Add($"[{c.Reference}] restitué mais conversation ouverte");
        }

        foreach (var n in world.Notifications)
        {
            if (n.CreatedAt > now) problems.Add($"notification « {n.Title} » dans le futur");
            if (n.ReportId is { } id && Rep(id) is { } r && n.CreatedAt < r.CreatedAt) problems.Add($"[{r.Reference}] notification antérieure à la déclaration");
        }

        foreach (var rc in world.Receipts.Values)
        {
            var r = Rep(rc.ReportId);
            if (r is null) { problems.Add($"reçu {rc.Reference} sans signalement"); continue; }
            if (r.Status != ReportStatus.Returned) problems.Add($"[{rc.Reference}] reçu pour un signalement non restitué");
            if (rc.ReturnedAt < r.CreatedAt) problems.Add($"[{rc.Reference}] restitution avant la déclaration");
            if (world.Matches.GetValueOrDefault(rc.ReportId) is { } m && rc.ReturnedAt < m.FoundAt) problems.Add($"[{rc.Reference}] restitution avant la correspondance");
        }

        return problems;
    }

    [Conditional("DEBUG")]
    public static void LogValidation(UserWorld world, IReadOnlyList<Report> reports, DateTime now)
    {
        var problems = Validate(world, reports, now);
        Debug.WriteLine(problems.Count == 0
            ? "[DemoWorld] Cohérence correspondances / messages / notifications / reçus : OK"
            : "[DemoWorld] INCOHÉRENCES : " + string.Join(" | ", problems));
    }
}
