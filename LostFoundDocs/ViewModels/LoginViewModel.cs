using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private string _phone = string.Empty;
    private string _password = string.Empty;
    private string _phoneError = string.Empty;
    private string _passwordError = string.Empty;
    private string _formError = string.Empty;

    public LoginViewModel(IAuthService auth, INavigationService nav)
    {
        _auth = auth;
        _nav = nav;
        SignInCommand = new AsyncCommand(SignInAsync);
        UseDemoCommand = new Command(UseDemoAccount);
        RegisterCommand = new AsyncCommand(() => _nav.GoToAsync(Routes.Register));
        ForgotPasswordCommand = new AsyncCommand(() => _nav.GoToAsync(Routes.ForgotPassword));
    }

    public ICommand SignInCommand { get; }
    public ICommand UseDemoCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand ForgotPasswordCommand { get; }

    public string Phone
    {
        get => _phone;
        set { if (SetProperty(ref _phone, value)) { PhoneError = string.Empty; FormError = string.Empty; } }
    }

    public string Password
    {
        get => _password;
        set { if (SetProperty(ref _password, value)) { PasswordError = string.Empty; FormError = string.Empty; } }
    }

    public string PhoneError { get => _phoneError; set => SetProperty(ref _phoneError, value); }
    public string PasswordError { get => _passwordError; set => SetProperty(ref _passwordError, value); }

    public string FormError
    {
        get => _formError;
        set { if (SetProperty(ref _formError, value)) OnPropertyChanged(nameof(HasFormError)); }
    }

    public bool HasFormError => !string.IsNullOrEmpty(FormError);
    public string SignInText => IsBusy ? "Connexion…" : "Se connecter";

    // SIMULATION : le compte de démonstration est affiché pour être identifiable.
    public string DemoHint => $"Téléphone : {DemoSettings.DemoPhoneDisplay}\nMot de passe : {DemoSettings.DemoPassword}";

    private void UseDemoAccount()
    {
        Phone = DemoSettings.DemoPhoneDisplay;
        Password = DemoSettings.DemoPassword;
    }

    private async Task SignInAsync()
    {
        FormError = string.Empty;
        PhoneError = FieldRules.Phone(Phone) ?? string.Empty;
        PasswordError = FieldRules.PasswordRequired(Password) ?? string.Empty;
        if (PhoneError.Length > 0 || PasswordError.Length > 0) return;

        SetBusy(true);
        try
        {
            var result = await _auth.SignInAsync(Phone, Password);
            if (result.Success)
            {
                Password = string.Empty;
                await _nav.GoToAsync("//" + Routes.Home);
            }
            else
            {
                Password = string.Empty;
                FormError = "Numéro ou mot de passe incorrect. Vérifiez vos informations ou utilisez le compte de démonstration.";
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        IsBusy = busy;
        OnPropertyChanged(nameof(SignInText));
    }
}
