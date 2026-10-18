using System.Collections.ObjectModel;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reports;
    private ReportKind _kind;
    public ReportsViewModel(IReportService reports, INavigationService nav, IDialogService dialogs)
    {
        _reports = reports;
        LostCommand = new Command(() => { _kind = ReportKind.Lost; Refresh(); });
        FoundCommand = new Command(() => { _kind = ReportKind.Found; Refresh(); });
        OpenCommand = new AsyncCommand<Report>(r => r is null ? Task.CompletedTask : nav.GoToAsync(Routes.ReportDetails, new Dictionary<string, object> { ["reportId"] = r.Id }));
        NewCommand = new AsyncCommand(async () =>
        {
            var choice = await dialogs.ChooseAsync("Nouveau signalement", "Annuler", "J’ai perdu un document", "J’ai trouvé un document");
            if (choice is null or "Annuler") return;
            await nav.GoToAsync(choice == "J’ai perdu un document" ? Routes.DeclareLost : Routes.DeclareFound);
        });
    }
    public ObservableCollection<Report> Items { get; } = new();
    public ICommand LostCommand { get; }
    public ICommand FoundCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public bool IsEmpty => Items.Count == 0;
    public string LostLabel => $"Perdus ({_reports.Reports.Count(r => r.Kind == ReportKind.Lost)})";
    public string FoundLabel => $"Trouvés ({_reports.Reports.Count(r => r.Kind == ReportKind.Found)})";
    public string FilterLabel => _kind == ReportKind.Lost ? "Documents perdus" : "Documents trouvés";
    public void Refresh()
    {
        Items.Clear();
        foreach (var report in _reports.Reports.Where(r => r.Kind == _kind)) Items.Add(report);
        OnPropertyChanged(string.Empty);
    }
}
