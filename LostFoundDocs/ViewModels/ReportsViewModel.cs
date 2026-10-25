using System.Collections.ObjectModel;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

/// <summary>Liste « Mes signalements » : onglets Perdus / Trouvés, filtre par statut, états vides.</summary>
public sealed class ReportsViewModel : ViewModelBase
{
    public const string FilterAll = "Tous";
    public const string FilterActive = "Actifs";
    public const string FilterDone = "Terminés";

    private readonly IReportService _reports;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private ReportKind _kind = ReportKind.Lost;
    private string? _filter = FilterAll;

    public ReportsViewModel(IReportService reports, INavigationService nav, IDialogService dialogs)
    {
        _reports = reports;
        _nav = nav;
        _dialogs = dialogs;
        SelectLostCommand = new Command(() => SelectKind(ReportKind.Lost));
        SelectFoundCommand = new Command(() => SelectKind(ReportKind.Found));
        NewReportCommand = new AsyncCommand(NewReportAsync);
        DeclareCommand = new AsyncCommand(() => _nav.GoToAsync(_kind == ReportKind.Lost ? Routes.DeclareLost : Routes.DeclareFound));
        OpenReportCommand = new AsyncCommand<Report>(r => r is null
            ? Task.CompletedTask
            : _nav.GoToAsync(Routes.ReportDetails, new Dictionary<string, object> { ["reportId"] = r.Id }));
    }

    public ObservableCollection<Report> Items { get; } = new();
    public IList<string> FilterOptions { get; } = new List<string> { FilterAll, FilterActive, FilterDone };

    public ICommand SelectLostCommand { get; }
    public ICommand SelectFoundCommand { get; }
    public ICommand NewReportCommand { get; }
    public ICommand DeclareCommand { get; }
    public ICommand OpenReportCommand { get; }

    public string? SelectedFilter
    {
        get => _filter;
        set { if (SetProperty(ref _filter, value ?? FilterAll)) Refresh(); }
    }

    public bool IsLostTab => _kind == ReportKind.Lost;
    public bool IsFoundTab => _kind == ReportKind.Found;
    public int LostCount { get; private set; }
    public int FoundCount { get; private set; }
    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => !HasItems;

    public string EmptyTitle => _kind == ReportKind.Lost ? "Aucun document perdu" : "Aucun document trouvé";
    public string EmptyMessage => SelectedFilter == FilterAll || SelectedFilter is null
        ? (_kind == ReportKind.Lost
            ? "Vous n’avez signalé aucune perte pour l’instant."
            : "Vous n’avez signalé aucun document trouvé pour l’instant.")
        : $"Aucun signalement ne correspond au filtre « {SelectedFilter} ».";
    public string EmptyAction => _kind == ReportKind.Lost ? "Déclarer un document perdu" : "Déclarer un document trouvé";

    private void SelectKind(ReportKind kind)
    {
        if (_kind == kind) return;
        _kind = kind;
        Refresh();
    }

    public void Refresh()
    {
        var all = _reports.Reports;
        LostCount = all.Count(r => r.Kind == ReportKind.Lost);
        FoundCount = all.Count(r => r.Kind == ReportKind.Found);

        IEnumerable<Report> query = all.Where(r => r.Kind == _kind);
        query = SelectedFilter switch
        {
            FilterActive => query.Where(r => !r.IsTerminal),
            FilterDone => query.Where(r => r.IsTerminal),
            _ => query,
        };

        Items.Clear();
        foreach (var r in query.OrderByDescending(r => r.CreatedAt)) Items.Add(r);
        RaiseAll();
    }

    private async Task NewReportAsync()
    {
        var choice = await _dialogs.ChooseAsync("Nouveau signalement — que souhaitez-vous déclarer ?", "Annuler",
            "J’ai perdu un document", "J’ai trouvé un document");
        if (choice is null) return;
        await _nav.GoToAsync(choice.Contains("perdu") ? Routes.DeclareLost : Routes.DeclareFound);
    }
}
