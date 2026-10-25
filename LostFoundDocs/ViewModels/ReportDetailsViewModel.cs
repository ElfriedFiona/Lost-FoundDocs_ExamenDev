using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;
using LostFoundDocs.UI;

namespace LostFoundDocs.ViewModels;

public sealed record InfoRow(string Label, string Value);

/// <summary>Détails d'un signalement : informations, chronologie et actions disponibles selon le statut.</summary>
public sealed class ReportDetailsViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IReportService _reports;
    private readonly IMatchService _matches;
    private readonly IConversationService _conversations;
    private readonly IRestitutionService _restitution;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private Guid _id;
    private Report? _report;

    public ReportDetailsViewModel(IReportService reports, IMatchService matches, IConversationService conversations,
                                  IRestitutionService restitution, INavigationService nav, IDialogService dialogs)
    {
        _reports = reports; _matches = matches; _conversations = conversations;
        _restitution = restitution; _nav = nav; _dialogs = dialogs;

        SearchCommand = new AsyncCommand(() => Go(Routes.Search));
        SeeMatchCommand = new AsyncCommand(() => Go(Routes.MatchDetails));
        OrganizeCommand = new AsyncCommand(() => Go(Routes.Restitution));
        ProofCommand = new AsyncCommand(() => Go(Routes.Receipt));
        ConversationCommand = new AsyncCommand(OpenConversationAsync);
        CancelReportCommand = new AsyncCommand(CancelReportAsync);
        CloseRecoveredCommand = new AsyncCommand(CloseRecoveredAsync);
    }

    public ICommand SearchCommand { get; }
    public ICommand SeeMatchCommand { get; }
    public ICommand OrganizeCommand { get; }
    public ICommand ProofCommand { get; }
    public ICommand ConversationCommand { get; }
    public ICommand CancelReportCommand { get; }
    public ICommand CloseRecoveredCommand { get; }

    public string Title => _report?.Title ?? string.Empty;
    public string IconGlyph => _report is null ? Icons.FileText : Presentation.Icon(_report.Type);
    public string Reference => _report is null ? string.Empty : $"Réf. {_report.Reference}";
    public string StatusLabel => _report?.StatusLabel ?? string.Empty;
    public BadgeKind StatusKind => _report is null ? BadgeKind.Neutral : Presentation.Kind(_report.Status);
    public bool StatusShowCheck => _report?.Status == ReportStatus.Returned;

    public IList<InfoRow> Rows
    {
        get
        {
            var rows = new List<InfoRow>();
            if (_report is null) return rows;
            var r = _report;
            rows.Add(new InfoRow("Type de document", r.Title));
            if (!string.IsNullOrWhiteSpace(r.HolderName)) rows.Add(new InfoRow("Nom", NameFormat.Capitalize(r.HolderName)));
            if (!string.IsNullOrEmpty(r.MaskedNumber)) rows.Add(new InfoRow(DocumentTypes.Get(r.Type).NumberLabel, r.MaskedNumber));
            if (r.BirthDate is { } b) rows.Add(new InfoRow("Date de naissance", $"••/••/{b.Year}"));
            if (!string.IsNullOrWhiteSpace(r.Issuer)) rows.Add(new InfoRow("Organisme", r.Issuer!));
            rows.Add(new InfoRow(r.Kind == ReportKind.Lost ? "Date de perte" : "Date de découverte", DateFormat.Long(r.EventDate)));
            rows.Add(new InfoRow("Lieu", r.LocationLabel));
            if (!string.IsNullOrWhiteSpace(r.Notes)) rows.Add(new InfoRow("Informations complémentaires", r.Notes!));
            if (!string.IsNullOrEmpty(r.PhotoPath)) rows.Add(new InfoRow("Photo", r.PhotoPath!));
            rows.Add(new InfoRow("Déclaré le", $"{DateFormat.Short(r.CreatedAt)} à {DateFormat.Time(r.CreatedAt)}"));
            return rows;
        }
    }

    public IList<string> Timeline
    {
        get
        {
            var list = new List<string>();
            if (_report is null) return list;
            var r = _report;
            list.Add($"Signalement enregistré · {DateFormat.Short(r.CreatedAt)} {DateFormat.Time(r.CreatedAt)}");
            if (_matches.GetMatch(_id) is { } m)
                list.Add($"Correspondance trouvée · {DateFormat.Short(m.FoundAt)} {DateFormat.Time(m.FoundAt)}");
            if (_restitution.GetProposal(_id) is { } p)
                list.Add($"Rendez-vous {(p.Accepted ? "confirmé" : "proposé")} · {p.Place}, {p.SlotLabel}");
            if (r.Status is ReportStatus.Returned or ReportStatus.Expired or ReportStatus.Cancelled or ReportStatus.MatchDisputed)
                list.Add($"{r.StatusLabel} · {DateFormat.Short(r.UpdatedAt)} {DateFormat.Time(r.UpdatedAt)}");
            return list;
        }
    }

    // ----- actions disponibles -----
    private Conversation? ConversationOrNull => _conversations.GetForReport(_id);
    private bool HasMatch => _matches.GetMatch(_id) is not null;
    public bool CanSearch => _report?.Status == ReportStatus.Searching;
    public string SearchText => _matches.GetOutcome(_id) is null ? "Lancer la recherche" : "Relancer la recherche";
    public bool CanSeeMatch => HasMatch && _report?.Status is ReportStatus.MatchFound or ReportStatus.MatchConfirmed or ReportStatus.MatchDisputed;
    public bool CanOpenConversation => ConversationOrNull is not null;
    public bool CanOrganize => _report?.Status is ReportStatus.MatchFound or ReportStatus.MatchConfirmed;
    public bool CanSeeProof => _report?.Status == ReportStatus.Returned && _restitution.GetReceipt(_id) is not null;
    public bool CanCloseRecovered => _report?.Status == ReportStatus.MatchDisputed;
    public bool CanCancel => _report is { IsTerminal: false };
    public bool HasNoActions => !(CanSearch || CanSeeMatch || CanOpenConversation || CanOrganize || CanSeeProof || CanCloseRecovered || CanCancel);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id)
        {
            _id = id;
            _ = LoadAsync();
        }
    }

    /// <summary>Recharge les données (appelé à l'affichage : le statut a pu changer dans un autre écran).</summary>
    public async Task LoadAsync()
    {
        if (_id == Guid.Empty) return;
        _report = await _reports.GetAsync(_id);
        RaiseAll();
    }

    private Task Go(string route) => _nav.GoToAsync(route, new Dictionary<string, object> { ["reportId"] = _id });

    private async Task OpenConversationAsync()
    {
        if (ConversationOrNull is { } c)
            await _nav.GoToAsync(Routes.Conversation, new Dictionary<string, object> { ["conversationId"] = c.Id });
    }

    private async Task CancelReportAsync()
    {
        var ok = await _dialogs.ConfirmAsync("Annuler ce signalement ?",
            "Il ne sera plus recherché ni comparé aux nouveaux documents. Cette action est définitive.", "Annuler le signalement", "Conserver");
        if (!ok) return;
        if (!await _reports.CancelAsync(_id))
            await _dialogs.AlertAsync("Action impossible", "Ce signalement ne peut plus être annulé dans son état actuel.");
        await LoadAsync();
    }

    private async Task CloseRecoveredAsync()
    {
        var ok = await _dialogs.ConfirmAsync("Clôturer le signalement ?",
            "Vous indiquez avoir déjà récupéré votre document.", "Clôturer", "Annuler");
        if (!ok) return;
        await _reports.UpdateStatusAsync(_id, ReportStatus.Returned);
        await LoadAsync();
    }
}
