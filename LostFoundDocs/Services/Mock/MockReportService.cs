using System.Diagnostics;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// SIMULATION (frontend uniquement) : signalements gardés en mémoire, par utilisateur.
/// Garanties : un signalement est enregistré une seule fois (idempotence du brouillon) et son statut évolue
/// sur le même objet, sans doublon.
/// </summary>
public sealed class MockReportService : IReportService
{
    private readonly IAuthService _auth;
    private readonly Dictionary<Guid, List<Report>> _store = new();
    private readonly HashSet<string> _references = new();
    private readonly object _gate = new();
    private int _nextReference = 10001;

    public MockReportService(IAuthService auth)
    {
        _auth = auth;
        _auth.SessionChanged += (_, _) =>
        {
            if (!_auth.IsSignedIn) _store.Clear();
            ReportsChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler? ReportsChanged;

    public IReadOnlyList<Report> Reports
    {
        get
        {
            lock (_gate)
            {
                return CurrentList().OrderByDescending(r => r.CreatedAt).ToList();
            }
        }
    }

    public Task<Report?> GetAsync(Guid id)
    {
        lock (_gate)
        {
            return Task.FromResult(CurrentList().FirstOrDefault(r => r.Id == id));
        }
    }

    public async Task<Report> SubmitAsync(ReportDraft draft)
    {
        var user = _auth.CurrentUser ?? throw new InvalidOperationException("Session expirée.");
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        if (_auth.CurrentUser?.Id != user.Id) throw new InvalidOperationException("La session a changé.");
        lock (_gate)
        {
            var list = CurrentList();

            // Idempotence : ce brouillon a déjà été enregistré -> on renvoie le même signalement.
            if (draft.SubmittedReportId is { } existingId && list.FirstOrDefault(r => r.Id == existingId) is { } existing)
            {
                return existing;
            }

            var error = DeclarationRules.Validate(draft, 0, Clock.Today).Values.FirstOrDefault();
            if (error is not null) throw new ArgumentException(error, nameof(draft));

            var now = Clock.Now;
            var report = new Report
            {
                UserId = _auth.CurrentUser!.Id,
                Reference = NextReference(now),
                Kind = draft.Kind,
                Type = draft.Type!.Value,
                CustomTitle = draft.CustomTitle,
                HolderName = draft.HolderName.Trim(),
                Number = draft.Number,
                BirthDate = draft.BirthDate,
                Issuer = draft.Issuer,
                EventDate = draft.EventDate!.Value.Date,
                City = draft.City,
                Place = draft.Place.Trim(),
                Notes = draft.Notes,
                PhotoPath = draft.PhotoPath,
                CreatedAt = now,
                UpdatedAt = now,
                Status = ReportStatus.Searching,
            };
            report.History.Add(new ReportStatusChange(ReportStatus.Searching, now));
            list.Insert(0, report);
            draft.SubmittedReportId = report.Id;
            ReportsChanged?.Invoke(this, EventArgs.Empty);
            return report;
        }
    }

    public async Task<Report> UpdateStatusAsync(Guid id, ReportStatus newStatus)
    {
        await Task.Delay(DemoSettings.SimulatedLatencyMs / 2);
        lock (_gate)
        {
            var report = CurrentList().FirstOrDefault(r => r.Id == id)
                         ?? throw new KeyNotFoundException("Signalement introuvable.");
            if (report.Status == newStatus) return report;
            if (!ReportStatusRules.CanTransition(report.Status, newStatus))
            {
                throw new InvalidOperationException($"Transition interdite : {report.Status} → {newStatus}.");
            }
            report.Status = newStatus;
            report.UpdatedAt = Clock.Now;
            report.History.Add(new ReportStatusChange(newStatus, report.UpdatedAt));
            ReportsChanged?.Invoke(this, EventArgs.Empty);
            return report;
        }
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        try
        {
            await UpdateStatusAsync(id, ReportStatus.Cancelled);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public Task ResetAsync(bool populated)
    {
        var user = _auth.CurrentUser ?? throw new InvalidOperationException("Session expirée.");
        _store[user.Id] = populated ? DemoData.CreateReportsFor(user.Id, Clock.Now) : new List<Report>();
        foreach (var report in _store[user.Id]) _references.Add(report.Reference);
        ReportsChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private List<Report> CurrentList()
    {
        var user = _auth.CurrentUser;
        if (user is null) return new List<Report>();
        if (!_store.TryGetValue(user.Id, out var list))
        {
            // Compte de démonstration : jeu de données fictif ; nouveau compte : liste vide (état vide à tester).
            list = user.IsDemoAccount ? DemoData.CreateReportsFor(user.Id, Clock.Now) : new List<Report>();
            _store[user.Id] = list;
            foreach (var r in list) _references.Add(r.Reference);
            LogValidation(list);
        }
        return list;
    }

    private string NextReference(DateTime now)
    {
        string reference;
        do
        {
            reference = $"LF-{now.Year}-{_nextReference++:D6}";
        } while (!_references.Add(reference));
        return reference;
    }

    [Conditional("DEBUG")]
    private static void LogValidation(IEnumerable<Report> reports)
    {
        var problems = DemoDataValidator.Validate(reports, Clock.Now);
        Debug.WriteLine(problems.Count == 0
            ? "[DemoData] Cohérence chronologique : OK"
            : "[DemoData] INCOHÉRENCES : " + string.Join(" | ", problems));
    }
}
