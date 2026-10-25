using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

/// <summary>SIMULATION : la progression est animée localement ; la recherche réelle sera faite par le backend.</summary>
public sealed class SearchViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IMatchService _match;
    private readonly INavigationService _nav;
    private Guid _reportId;
    private bool _step1, _step2, _step3, _found, _started;

    public SearchViewModel(IMatchService match, INavigationService nav)
    {
        _match = match;
        _nav = nav;
        ViewResultCommand = new AsyncCommand(ViewResultAsync, () => HasResult);
        CloseCommand = new AsyncCommand(() => _nav.GoToAsync("//" + Routes.Home));
    }

    public ICommand ViewResultCommand { get; }
    public ICommand CloseCommand { get; }

    public IList<string> Criteria { get; } = new List<string>
        { "Type", "Nom", "Numéro", "Date de naissance", "Établissement", "Ville", "Date" };

    public bool Step1Done { get => _step1; private set { if (SetProperty(ref _step1, value)) OnPropertyChanged(nameof(Step1Text)); } }
    public bool Step2Done { get => _step2; private set { if (SetProperty(ref _step2, value)) OnPropertyChanged(nameof(Step2Text)); } }
    public bool Step3Done
    {
        get => _step3;
        private set
        {
            if (!SetProperty(ref _step3, value)) return;
            OnPropertyChanged(nameof(Step3Text));
            OnPropertyChanged(nameof(HasResult));
            OnPropertyChanged(nameof(IsRunning));
            (ViewResultCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool HasResult => Step3Done;
    public bool IsRunning => !Step3Done;
    public bool Found { get => _found; private set { if (SetProperty(ref _found, value)) { OnPropertyChanged(nameof(ResultKind)); OnPropertyChanged(nameof(ResultTitle)); OnPropertyChanged(nameof(ResultMessage)); } } }

    public string Step1Text => Step1Done ? "Signalement enregistré : terminé" : "Signalement enregistré…";
    public string Step2Text => Step2Done ? "Informations analysées : terminé" : "Informations analysées…";
    public string Step3Text => Step3Done ? "Recherche de correspondance : terminé" : "Recherche de correspondance…";

    public UI.BannerKind ResultKind => Found ? UI.BannerKind.Success : UI.BannerKind.Warning;
    public string ResultTitle => Found ? "Correspondance trouvée" : "Aucune correspondance pour l’instant";
    public string ResultMessage => Found
        ? "Un signalement compatible a été détecté."
        : "Nous continuerons la recherche automatiquement (simulation).";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_started) return;
        if (query.TryGetValue("reportId", out var value) && value is Guid id)
        {
            _started = true;
            _reportId = id;
            _ = RunAsync();
        }
    }

    private async Task RunAsync()
    {
        Step1Done = true;
        await Task.Delay(700);
        Step2Done = true;
        var outcome = await _match.SearchAsync(_reportId);
        Found = outcome.Found;
        Step3Done = true;
    }

    private Task ViewResultAsync() =>
        _nav.GoToAsync("../" + (Found ? Routes.MatchResult : Routes.NoMatch), new Dictionary<string, object> { ["reportId"] = _reportId });
}
