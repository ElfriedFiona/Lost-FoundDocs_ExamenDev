using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

public sealed class DeclarationConfirmationViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IReportService _reports;
    private readonly INavigationService _nav;
    private Report? _report;

    public DeclarationConfirmationViewModel(IReportService reports, INavigationService nav)
    {
        _reports = reports;
        _nav = nav;
        StartSearchCommand = new AsyncCommand(() => _report is null
            ? Task.CompletedTask
            : _nav.GoToAsync("../" + Routes.Search, new Dictionary<string, object> { ["reportId"] = _report.Id }));
        SeeReportsCommand = new AsyncCommand(() => _nav.GoToAsync("//" + Routes.Reports));
        CloseCommand = new AsyncCommand(() => _nav.GoToAsync("//" + Routes.Home));
    }

    public ICommand StartSearchCommand { get; }
    public ICommand SeeReportsCommand { get; }
    public ICommand CloseCommand { get; }

    public string Title => _report?.Kind == ReportKind.Lost ? "Document perdu déclaré" : "Document trouvé déclaré";
    public string Message => _report?.Kind == ReportKind.Lost
        ? "Votre signalement est enregistré. Lancez la recherche d’une correspondance parmi les documents déjà déclarés."
        : "Merci de contribuer à restituer ce document à son propriétaire. Lancez la recherche d’un signalement de perte compatible.";
    public string Reference => _report?.Reference ?? string.Empty;
    public string DocumentTitle => _report?.Title ?? string.Empty;
    public string EventLabel => _report?.DateLabelLong ?? string.Empty;
    public string LocationLabel => _report?.LocationLabel ?? string.Empty;
    public string StatusLabel => _report?.StatusLabel ?? string.Empty;
    public string MaskedNumber => _report?.MaskedNumber ?? string.Empty;
    public bool HasNumber => !string.IsNullOrEmpty(MaskedNumber);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id) _ = LoadAsync(id);
    }

    private async Task LoadAsync(Guid id)
    {
        _report = await _reports.GetAsync(id);
        foreach (var n in new[] { nameof(Title), nameof(Message), nameof(Reference), nameof(DocumentTitle), nameof(EventLabel),
                                  nameof(LocationLabel), nameof(StatusLabel), nameof(MaskedNumber), nameof(HasNumber) })
            OnPropertyChanged(n);
    }
}
