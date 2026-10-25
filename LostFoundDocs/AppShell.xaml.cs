using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Views.Auth;
using LostFoundDocs.Views.Declaration;

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

        // Les routes livrées cessent d'afficher « Bientôt disponible ».
        Routes.MarkAvailable(Routes.DeclareLost, Routes.DeclareFound, Routes.DeclarationConfirmation);
    }
}
