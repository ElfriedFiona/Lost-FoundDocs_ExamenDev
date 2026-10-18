using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;
namespace LostFoundDocs.ViewModels;

public sealed class ProfileSettingsViewModel : ViewModelBase
{
    private readonly IProfileService _profile;
    private string _name;
    private string _email;
    private string _password = "";
    private string _confirmation = "";
    private string _currentPassword = "";
    private bool _faq;
    public event Action<string>? FocusRequested;
    public ProfileSettingsViewModel(IProfileService profile, IAuthService auth, IDialogService dialogs)
    {
        _profile = profile;
        _name = auth.CurrentUser?.FullName ?? "";
        _email = auth.CurrentUser?.Email ?? "";
        Phone = auth.CurrentUser?.DisplayPhone ?? "";
        SaveCommand = new AsyncCommand(async () =>
        {
            NameError = FieldRules.FullName(Name) ?? "";
            EmailError = FieldRules.OptionalEmail(Email) ?? "";
            OnPropertyChanged(string.Empty);
            if (NameError.Length > 0) { FocusRequested?.Invoke("Name"); return; }
            if (EmailError.Length > 0) { FocusRequested?.Invoke("Email"); return; }
            await profile.SaveInformationAsync(Name, Email);
            await dialogs.AlertAsync("Informations enregistrées", "Votre profil a été mis à jour pour cette session.");
        });
        PasswordCommand = new AsyncCommand(async () =>
        {
            CurrentPasswordError = FieldRules.Required(CurrentPassword, "Saisissez votre mot de passe actuel.") ?? "";
            PasswordError = FieldRules.NewPassword(Password) ?? "";
            ConfirmationError = FieldRules.PasswordConfirmation(Password, Confirmation) ?? "";
            OnPropertyChanged(string.Empty);
            if (CurrentPasswordError.Length > 0) { FocusRequested?.Invoke("CurrentPassword"); return; }
            if (PasswordError.Length > 0) { FocusRequested?.Invoke("Password"); return; }
            if (ConfirmationError.Length > 0) { FocusRequested?.Invoke("Confirmation"); return; }
            await profile.ChangePasswordAsync(CurrentPassword, Password, Confirmation);
            Password = Confirmation = CurrentPassword = "";
            await dialogs.AlertAsync("Mot de passe modifié", "Modification simulée enregistrée pour cette session.");
        });
        EmptyCommand = new AsyncCommand(() => ResetAsync(false, dialogs));
        PopulateCommand = new AsyncCommand(() => ResetAsync(true, dialogs));
        FaqCommand = new Command(() => { _faq = !_faq; OnPropertyChanged(nameof(ShowFaq)); });
        SupportCommand = new AsyncCommand(() => dialogs.AlertAsync("Assistance simulée", "Aucun message n’a été envoyé. Dans la version finale, ce bouton contactera l’assistance."));
    }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Phone { get; }
    public string CurrentPassword { get => _currentPassword; set => SetProperty(ref _currentPassword, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string Confirmation { get => _confirmation; set => SetProperty(ref _confirmation, value); }
    public string NameError { get; private set; } = "";
    public string EmailError { get; private set; } = "";
    public string CurrentPasswordError { get; private set; } = "";
    public string PasswordError { get; private set; } = "";
    public string ConfirmationError { get; private set; } = "";
    public bool ShowFaq => _faq;
    public bool MatchNotifications { get => _profile.NotificationPreferences[0]; set => _profile.NotificationPreferences[0] = value; }
    public bool MessageNotifications { get => _profile.NotificationPreferences[1]; set => _profile.NotificationPreferences[1] = value; }
    public bool ReturnNotifications { get => _profile.NotificationPreferences[2]; set => _profile.NotificationPreferences[2] = value; }
    public bool ReminderNotifications { get => _profile.NotificationPreferences[3]; set => _profile.NotificationPreferences[3] = value; }
    public int MatchModeIndex { get => _profile.MatchModeIndex; set { if (value >= 0) _profile.MatchModeIndex = value; } }
    public string[] MatchModes { get; } = ["Automatique : type et ville", "Toujours une correspondance", "Jamais de correspondance"];
    public ICommand SaveCommand { get; }
    public ICommand PasswordCommand { get; }
    public ICommand EmptyCommand { get; }
    public ICommand PopulateCommand { get; }
    public ICommand FaqCommand { get; }
    public ICommand SupportCommand { get; }
    private async Task ResetAsync(bool populated, IDialogService dialogs)
    {
        if (!await dialogs.ConfirmAsync("Réinitialiser la démonstration ?", "Les signalements et échanges de cette session seront remplacés.", "Réinitialiser")) return;
        await _profile.ResetDemoAsync(populated);
        await dialogs.AlertAsync("Démonstration réinitialisée", populated ? "Le compte contient les signalements fictifs." : "Le compte est maintenant vide.");
    }
}
