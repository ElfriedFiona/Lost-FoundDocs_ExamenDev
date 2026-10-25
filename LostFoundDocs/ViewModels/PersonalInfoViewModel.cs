using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

public sealed class PersonalInfoViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private string _fullName, _email;
    private string _nameError = string.Empty, _emailError = string.Empty, _success = string.Empty;

    public PersonalInfoViewModel(IAuthService auth)
    {
        _auth = auth;
        _fullName = auth.CurrentUser?.FullName ?? string.Empty;
        _email = auth.CurrentUser?.Email ?? string.Empty;
        SaveCommand = new AsyncCommand(SaveAsync);
    }

    public ICommand SaveCommand { get; }
    public string DisplayPhone => _auth.CurrentUser?.DisplayPhone ?? string.Empty;
    public string PhoneStatus => _auth.CurrentUser?.IsPhoneVerified == true ? "Numéro vérifié. Il ne peut pas être modifié dans la démonstration." : "Numéro non vérifié (simulation).";

    public string FullName { get => _fullName; set { if (SetProperty(ref _fullName, value)) { NameError = string.Empty; Success = string.Empty; } } }
    public string Email { get => _email; set { if (SetProperty(ref _email, value)) { EmailError = string.Empty; Success = string.Empty; } } }
    public string NameError { get => _nameError; private set => SetProperty(ref _nameError, value); }
    public string EmailError { get => _emailError; private set => SetProperty(ref _emailError, value); }
    public string Success { get => _success; private set { if (SetProperty(ref _success, value)) OnPropertyChanged(nameof(HasSuccess)); } }
    public bool HasSuccess => !string.IsNullOrEmpty(Success);

    private async Task SaveAsync()
    {
        NameError = FieldRules.FullName(FullName) ?? string.Empty;
        EmailError = FieldRules.OptionalEmail(Email) ?? string.Empty;
        if (NameError.Length > 0 || EmailError.Length > 0) return;
        await _auth.UpdateProfileAsync(FullName, Email);
        Success = "Informations enregistrées (simulation : elles restent sur l’appareil).";
    }
}
