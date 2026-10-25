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
    }
}
