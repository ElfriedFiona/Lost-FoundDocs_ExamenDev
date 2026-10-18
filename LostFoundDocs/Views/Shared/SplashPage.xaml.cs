using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.Views.Shared;

public partial class SplashPage : ContentPage
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private bool _started;

    public SplashPage(IAuthService auth, INavigationService nav)
    {
        InitializeComponent();
        _auth = auth;
        _nav = nav;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        await Task.Delay(1500);
        await _nav.GoToAsync("//" + (_auth.IsSignedIn ? Routes.Home : Routes.Login));
    }
}
