using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

public sealed class SecurityViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private string _current = string.Empty, _new = string.Empty, _confirm = string.Empty;
    private string _currentError = string.Empty, _newError = string.Empty, _confirmError = string.Empty, _success = string.Empty;

    public SecurityViewModel(IAuthService auth)
    {
        _auth = auth;
        SaveCommand = new AsyncCommand(SaveAsync);
    }

    public ICommand SaveCommand { get; }

    public string CurrentPassword { get => _current; set { if (SetProperty(ref _current, value)) { CurrentError = string.Empty; Success = string.Empty; } } }
    public string NewPassword { get => _new; set { if (SetProperty(ref _new, value)) { NewError = string.Empty; Success = string.Empty; } } }
    public string ConfirmPassword { get => _confirm; set { if (SetProperty(ref _confirm, value)) { ConfirmError = string.Empty; Success = string.Empty; } } }
    public string CurrentError { get => _currentError; private set => SetProperty(ref _currentError, value); }
    public string NewError { get => _newError; private set => SetProperty(ref _newError, value); }
    public string ConfirmError { get => _confirmError; private set => SetProperty(ref _confirmError, value); }
    public string Success { get => _success; private set { if (SetProperty(ref _success, value)) OnPropertyChanged(nameof(HasSuccess)); } }
    public bool HasSuccess => !string.IsNullOrEmpty(Success);

    private async Task SaveAsync()
    {
        CurrentError = FieldRules.PasswordRequired(CurrentPassword) ?? string.Empty;
        NewError = FieldRules.NewPassword(NewPassword) ?? string.Empty;
        ConfirmError = FieldRules.PasswordConfirmation(NewPassword, ConfirmPassword) ?? string.Empty;
        if (NewError.Length == 0 && NewPassword == CurrentPassword && CurrentPassword.Length > 0)
            NewError = "Le nouveau mot de passe doit être différent de l’actuel.";
        if (CurrentError.Length > 0 || NewError.Length > 0 || ConfirmError.Length > 0) return;

        var result = await _auth.ChangePasswordAsync(CurrentPassword, NewPassword);
        if (!result.Success)
        {
            CurrentError = "Mot de passe actuel incorrect.";
            return;
        }
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        Success = "Mot de passe modifié (simulation).";
    }
}
