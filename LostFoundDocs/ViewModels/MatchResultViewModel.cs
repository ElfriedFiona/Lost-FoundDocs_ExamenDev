using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

public sealed class MatchResultViewModel : MatchViewModelBase
{
    public MatchResultViewModel(IReportService reports, IMatchService matches, IConversationService conversations, INavigationService nav)
        : base(reports, matches, conversations, nav, explanationOpen: false)
    {
        PrimaryCommand = new AsyncCommand(PrimaryAsync);
    }

    public ICommand PrimaryCommand { get; }

    public string Subtitle => IsOwnerSide
        ? "Une déclaration de document trouvé correspond à votre signalement."
        : "Un signalement de perte correspond au document que vous avez trouvé.";
    public bool ShowFinderBanner => !IsOwnerSide && !IsDisputed;

    /// <summary>Côté perte : « Voir la correspondance » ouvre le détail. Côté trouvaille : « Contacter le propriétaire » ouvre la conversation.</summary>
    public string PrimaryText => IsOwnerSide ? "Voir la correspondance" : ContactText;

    private Task PrimaryAsync()
    {
        if (Report is null) return Task.CompletedTask;
        return IsOwnerSide
            ? Nav.GoToAsync(Routes.MatchDetails, new Dictionary<string, object> { ["reportId"] = Report.Id })
            : ContactAsync();
    }
}
