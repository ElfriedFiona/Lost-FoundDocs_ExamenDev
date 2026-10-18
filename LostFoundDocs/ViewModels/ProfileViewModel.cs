using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

/// <summary>Identité, navigation du profil et déconnexion.</summary>
public sealed class ProfileViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _nav;

    public ProfileViewModel(IAuthService auth, IDialogService dialogs, INavigationService nav)
    {
        _auth = auth;
        _dialogs = dialogs;
        _nav = nav;
        SignOutCommand = new AsyncCommand(SignOutAsync);
        OpenPageCommand = new AsyncCommand<string>(route => route is null ? Task.CompletedTask : _nav.GoToAsync(route));
    }

    public ICommand SignOutCommand { get; }
    public ICommand OpenPageCommand { get; }

    public string FullName => _auth.CurrentUser?.FullName ?? string.Empty;
    public string DisplayPhone => _auth.CurrentUser?.DisplayPhone ?? string.Empty;
    public string Initials => _auth.CurrentUser?.Initials ?? string.Empty;
    public bool IsVerified => _auth.CurrentUser?.IsPhoneVerified ?? false;
    public bool IsDemoAccount => _auth.CurrentUser?.IsDemoAccount ?? false;

    public void Refresh()
    {
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(DisplayPhone));
        OnPropertyChanged(nameof(Initials));
        OnPropertyChanged(nameof(IsVerified));
        OnPropertyChanged(nameof(IsDemoAccount));
    }

    private async Task SignOutAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync("Se déconnecter ?", "Vous devrez vous reconnecter pour accéder à vos signalements.", "Se déconnecter");
        if (!confirmed) return;
        await _auth.SignOutAsync();
        await _nav.GoToAsync("//" + Routes.Login);
    }
}
