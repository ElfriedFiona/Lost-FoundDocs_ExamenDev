using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

public sealed class RegisterViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private string _fullName = string.Empty, _phone = string.Empty, _email = string.Empty;
    private string _password = string.Empty, _confirm = string.Empty;
    private string _nameError = string.Empty, _phoneError = string.Empty, _emailError = string.Empty;
    private string _passwordError = string.Empty, _confirmError = string.Empty;

    public RegisterViewModel(IAuthService auth, INavigationService nav)
    {
        _auth = auth;
        _nav = nav;
        RegisterCommand = new AsyncCommand(RegisterAsync);
    }

    public ICommand RegisterCommand { get; }

    public string FullName { get => _fullName; set { if (SetProperty(ref _fullName, value)) NameError = string.Empty; } }
    public string Phone { get => _phone; set { if (SetProperty(ref _phone, value)) PhoneError = string.Empty; } }
    public string Email { get => _email; set { if (SetProperty(ref _email, value)) EmailError = string.Empty; } }
    public string Password { get => _password; set { if (SetProperty(ref _password, value)) PasswordError = string.Empty; } }
    public string ConfirmPassword { get => _confirm; set { if (SetProperty(ref _confirm, value)) ConfirmError = string.Empty; } }

    public string NameError { get => _nameError; set => SetProperty(ref _nameError, value); }
    public string PhoneError { get => _phoneError; set => SetProperty(ref _phoneError, value); }
    public string EmailError { get => _emailError; set => SetProperty(ref _emailError, value); }
    public string PasswordError { get => _passwordError; set => SetProperty(ref _passwordError, value); }
    public string ConfirmError { get => _confirmError; set => SetProperty(ref _confirmError, value); }

    public string SubmitText => IsBusy ? "Création…" : "Créer mon compte";

    private async Task RegisterAsync()
    {
        NameError = FieldRules.FullName(FullName) ?? string.Empty;
        PhoneError = FieldRules.Phone(Phone) ?? string.Empty;
        EmailError = FieldRules.OptionalEmail(Email) ?? string.Empty;
        PasswordError = FieldRules.NewPassword(Password) ?? string.Empty;
        ConfirmError = FieldRules.PasswordConfirmation(Password, ConfirmPassword) ?? string.Empty;
        if (new[] { NameError, PhoneError, EmailError, PasswordError, ConfirmError }.Any(e => e.Length > 0)) return;

        IsBusy = true;
        OnPropertyChanged(nameof(SubmitText));
        try
        {
            var result = await _auth.RegisterAsync(new RegistrationRequest(FullName, Phone, Email, Password));
            if (result.Success)
            {
                await _nav.GoToAsync("//" + Routes.Home);
            }
            else if (result.Failure == AuthFailure.PhoneAlreadyUsed)
            {
                PhoneError = "Ce numéro est déjà associé à un compte. Connectez-vous ou utilisez un autre numéro.";
            }
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(SubmitText));
        }
    }
}
