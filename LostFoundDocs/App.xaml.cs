namespace LostFoundDocs;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light; // la maquette n'existe qu'en thème clair
    }

    protected override Window CreateWindow(IActivationState? activationState) => new Window(new AppShell());
}
