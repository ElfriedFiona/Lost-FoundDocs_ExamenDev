using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.UI;

namespace LostFoundDocs.ViewModels;

public sealed record ProfileMenuItem(string Title, string Glyph, string Route);

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
        OpenCommand = new AsyncCommand<string>(route => string.IsNullOrEmpty(route) ? Task.CompletedTask : _nav.GoToAsync(route));
    }

    public ICommand SignOutCommand { get; }
    public ICommand OpenCommand { get; }

    public IList<ProfileMenuItem> AccountItems { get; } = new List<ProfileMenuItem>
    {
        new("Informations personnelles", Icons.User, Routes.PersonalInfo),
        new("Sécurité", Icons.ShieldCheck, Routes.Security),
        new("Notifications", Icons.Bell, Routes.NotificationPrefs),
    };

    public IList<ProfileMenuItem> SupportItems { get; } = new List<ProfileMenuItem>
    {
        new("Confidentialité", Icons.Lock, Routes.Privacy),
        new("Aide", Icons.Box, Routes.Help),
        new("À propos de Lost&Found Docs", Icons.Info, Routes.About),
    };

    public string FullName => _auth.CurrentUser?.FullName ?? string.Empty;
    public string DisplayPhone => _auth.CurrentUser?.DisplayPhone ?? string.Empty;
    public string Initials => _auth.CurrentUser?.Initials ?? string.Empty;
    public bool IsVerified => _auth.CurrentUser?.IsPhoneVerified ?? false;
    public bool IsNotVerified => !IsVerified;
    public bool IsDemoAccount => _auth.CurrentUser?.IsDemoAccount ?? false;
    public string FooterText => "Lost&Found Docs · version 1.0 · Douala";

    public void Refresh()
    {
        foreach (var n in new[] { nameof(FullName), nameof(DisplayPhone), nameof(Initials), nameof(IsVerified), nameof(IsNotVerified), nameof(IsDemoAccount) })
            OnPropertyChanged(n);
    }

    private async Task SignOutAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync("Se déconnecter ?", "Vous devrez vous reconnecter pour accéder à vos signalements.", "Se déconnecter");
        if (!confirmed) return;
        await _auth.SignOutAsync();
        await _nav.GoToAsync("//" + Routes.Login);
    }
}
