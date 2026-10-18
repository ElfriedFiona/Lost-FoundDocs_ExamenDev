using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Views.Auth;

namespace LostFoundDocs;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Écrans empilés (hors onglets)
        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.ForgotPassword, typeof(ForgotPasswordPage));
        Routing.RegisterRoute(Routes.DeclareLost, typeof(Views.Declaration.DeclarationPage));
        Routing.RegisterRoute(Routes.DeclareFound, typeof(Views.Declaration.DeclarationPage));
        Routing.RegisterRoute(Routes.Confirmation, typeof(Views.Declaration.ConfirmationPage));
        Routing.RegisterRoute(Routes.ReportDetails, typeof(Views.Reports.ReportDetailsPage));
        Routing.RegisterRoute(Routes.Match, typeof(Views.Matching.MatchPage));
        Routing.RegisterRoute(Routes.MatchDetails, typeof(Views.Matching.MatchDetailsPage));
        Routing.RegisterRoute(Routes.Conversation, typeof(Views.Messages.ConversationPage));
        Routing.RegisterRoute(Routes.Notifications, typeof(Views.Notifications.NotificationsPage));
        Routing.RegisterRoute(Routes.Restitution, typeof(Views.Restitution.RestitutionPage));
        Routing.RegisterRoute(Routes.Returned, typeof(Views.Restitution.ReturnedPage));
        Routing.RegisterRoute(Routes.Receipt, typeof(Views.Restitution.ReceiptPage));
        Routing.RegisterRoute(Routes.Personal, typeof(Views.Profile.PersonalPage));
        Routing.RegisterRoute(Routes.Security, typeof(Views.Profile.SecurityPage));
        Routing.RegisterRoute(Routes.Preferences, typeof(Views.Profile.PreferencesPage));
        Routing.RegisterRoute(Routes.Privacy, typeof(Views.Profile.PrivacyPage));
        Routing.RegisterRoute(Routes.Help, typeof(Views.Profile.HelpPage));
        Routing.RegisterRoute(Routes.About, typeof(Views.Profile.AboutPage));
    }
}
