using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Views.Auth;
using LostFoundDocs.Views.Declaration;
using LostFoundDocs.Views.Matching;
using LostFoundDocs.Views.Reports;

namespace LostFoundDocs;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Écrans empilés (hors onglets)
        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.ForgotPassword, typeof(ForgotPasswordPage));
        Routing.RegisterRoute(Routes.DeclareLost, typeof(DeclareLostPage));
        Routing.RegisterRoute(Routes.DeclareFound, typeof(DeclareFoundPage));
        Routing.RegisterRoute(Routes.DeclarationConfirmation, typeof(DeclarationConfirmationPage));
        Routing.RegisterRoute(Routes.Search, typeof(SearchPage));
        Routing.RegisterRoute(Routes.MatchResult, typeof(MatchResultPage));
        Routing.RegisterRoute(Routes.NoMatch, typeof(NoMatchPage));
        Routing.RegisterRoute(Routes.MatchDetails, typeof(MatchDetailsPage));
        Routing.RegisterRoute(Routes.ReportDetails, typeof(ReportDetailsPage));

        // Les routes livrées cessent d'afficher « Bientôt disponible ».
        Routes.MarkAvailable(Routes.DeclareLost, Routes.DeclareFound, Routes.DeclarationConfirmation, Routes.Search, Routes.MatchResult, Routes.NoMatch, Routes.MatchDetails, Routes.ReportDetails);
    }
}
