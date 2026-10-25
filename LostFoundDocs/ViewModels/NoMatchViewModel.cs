using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

public sealed class NoMatchViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IMatchService _match;
    private readonly INavigationService _nav;
    private Guid _reportId;
    private string _retryMessage = string.Empty;

    public NoMatchViewModel(IMatchService match, INavigationService nav)
    {
        _match = match;
        _nav = nav;
        SeeReportCommand = new AsyncCommand(() => _nav.GoToAsync(Routes.ReportDetails, new Dictionary<string, object> { ["reportId"] = _reportId }));
        HomeCommand = new AsyncCommand(() => _nav.GoToAsync("//" + Routes.Home));
        RetryCommand = new AsyncCommand(RetryAsync);
    }

    public ICommand SeeReportCommand { get; }
    public ICommand HomeCommand { get; }
    public ICommand RetryCommand { get; }

    public string RetryMessage { get => _retryMessage; private set { if (SetProperty(ref _retryMessage, value)) OnPropertyChanged(nameof(HasRetryMessage)); } }
    public bool HasRetryMessage => !string.IsNullOrEmpty(RetryMessage);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id) _reportId = id;
    }

    private async Task RetryAsync()
    {
        RetryMessage = string.Empty;
        var outcome = await _match.SearchAsync(_reportId, force: true);
        if (outcome.Found)
            await _nav.GoToAsync("../" + Routes.MatchResult, new Dictionary<string, object> { ["reportId"] = _reportId });
        else
            RetryMessage = "Toujours aucune correspondance pour le moment (simulation).";
    }
}
