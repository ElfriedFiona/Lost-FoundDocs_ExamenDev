using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

/// <summary>Version minimale de la phase A (identité + déconnexion). Complétée en phase B.</summary>
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
    }

    public ICommand SignOutCommand { get; }

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
