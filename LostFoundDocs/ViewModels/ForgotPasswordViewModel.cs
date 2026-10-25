using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

public enum ResetStep { Phone, Code, NewPassword, Done }

/// <summary>SIMULATION : aucun SMS n'est envoyé ; le code de démonstration est affiché à l'écran.</summary>
public sealed class ForgotPasswordViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private ResetStep _step = ResetStep.Phone;
    private string _phone = string.Empty, _code = string.Empty, _password = string.Empty, _confirm = string.Empty;
    private string _phoneError = string.Empty, _codeError = string.Empty, _passwordError = string.Empty, _confirmError = string.Empty;

    public ForgotPasswordViewModel(IAuthService auth, INavigationService nav)
    {
        _auth = auth;
        _nav = nav;
        SendCodeCommand = new AsyncCommand(SendCodeAsync);
        VerifyCodeCommand = new AsyncCommand(VerifyCodeAsync);
        ResetCommand = new AsyncCommand(ResetAsync);
        BackToLoginCommand = new AsyncCommand(() => _nav.GoToAsync("//" + Routes.Login));
    }

    public ICommand SendCodeCommand { get; }
    public ICommand VerifyCodeCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand BackToLoginCommand { get; }

    public ResetStep Step
    {
        get => _step;
        private set
        {
            if (SetProperty(ref _step, value))
            {
                OnPropertyChanged(nameof(IsPhoneStep));
                OnPropertyChanged(nameof(IsCodeStep));
                OnPropertyChanged(nameof(IsPasswordStep));
                OnPropertyChanged(nameof(IsDoneStep));
                OnPropertyChanged(nameof(Subtitle));
            }
        }
    }

    public bool IsPhoneStep => Step == ResetStep.Phone;
    public bool IsCodeStep => Step == ResetStep.Code;
    public bool IsPasswordStep => Step == ResetStep.NewPassword;
    public bool IsDoneStep => Step == ResetStep.Done;

    public string Subtitle => Step switch
    {
        ResetStep.Phone => "Saisissez le numéro de téléphone de votre compte.",
        ResetStep.Code => "Saisissez le code reçu par SMS.",
        ResetStep.NewPassword => "Choisissez un nouveau mot de passe.",
        _ => "Votre mot de passe a été modifié.",
    };

    public string SimulationNotice => $"Simulation : aucun SMS n’est envoyé. Le code de démonstration est {DemoSettings.ResetCode}.";

    public string Phone { get => _phone; set { if (SetProperty(ref _phone, value)) PhoneError = string.Empty; } }
    public string Code { get => _code; set { if (SetProperty(ref _code, value)) CodeError = string.Empty; } }
    public string NewPassword { get => _password; set { if (SetProperty(ref _password, value)) PasswordError = string.Empty; } }
    public string ConfirmPassword { get => _confirm; set { if (SetProperty(ref _confirm, value)) ConfirmError = string.Empty; } }

    public string PhoneError { get => _phoneError; set => SetProperty(ref _phoneError, value); }
    public string CodeError { get => _codeError; set => SetProperty(ref _codeError, value); }
    public string PasswordError { get => _passwordError; set => SetProperty(ref _passwordError, value); }
    public string ConfirmError { get => _confirmError; set => SetProperty(ref _confirmError, value); }

    private async Task SendCodeAsync()
    {
        PhoneError = FieldRules.Phone(Phone) ?? string.Empty;
        if (PhoneError.Length > 0) return;
        IsBusy = true;
        try
        {
            await _auth.RequestPasswordResetAsync(Phone);
            Step = ResetStep.Code;
        }
        finally { IsBusy = false; }
    }

    private async Task VerifyCodeAsync()
    {
        CodeError = FieldRules.Required(Code, "Saisissez le code reçu.") ?? string.Empty;
        if (CodeError.Length > 0) return;
        IsBusy = true;
        try
        {
            var result = await _auth.VerifyResetCodeAsync(Phone, Code);
            if (result.Success) Step = ResetStep.NewPassword;
            else CodeError = "Code incorrect ou expiré.";
        }
        finally { IsBusy = false; }
    }

    private async Task ResetAsync()
    {
        PasswordError = FieldRules.NewPassword(NewPassword) ?? string.Empty;
        ConfirmError = FieldRules.PasswordConfirmation(NewPassword, ConfirmPassword) ?? string.Empty;
        if (PasswordError.Length > 0 || ConfirmError.Length > 0) return;
        IsBusy = true;
        try
        {
            var result = await _auth.ResetPasswordAsync(Phone, NewPassword);
            if (result.Success) Step = ResetStep.Done;
            else PasswordError = "La vérification a expiré. Recommencez depuis le début.";
        }
        finally { IsBusy = false; }
    }
}
