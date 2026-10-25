using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.ViewModels;

public sealed class MatchDetailsViewModel : MatchViewModelBase
{
    public MatchDetailsViewModel(IReportService reports, IMatchService matches, IConversationService conversations, INavigationService nav)
        : base(reports, matches, conversations, nav, explanationOpen: true)
    {
    }

    public string Heading => IsOwnerSide ? "Document trouvé compatible" : "Signalement de perte compatible";
    public string SignalementSummary => Report is null ? string.Empty : $"Votre signalement : {Report.Title} · Réf. {Report.Reference}";
}
