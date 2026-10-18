using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// SIMULATION (frontend uniquement) : comptes en mémoire, aucun serveur, aucun SMS.
/// - Compte de démonstration : voir <see cref="DemoSettings"/>.
/// - Les comptes créés via l'inscription existent le temps de la session de l'application.
/// - Identifiants incorrects : réponse volontairement générique (ne révèle pas si le numéro existe).
/// </summary>
public sealed class MockAuthService : IAuthService
{
    private readonly List<UserAccount> _accounts = new();
    private readonly HashSet<string> _verifiedResets = new();

    public MockAuthService()
    {
        _accounts.Add(new UserAccount
        {
            FullName = DemoSettings.DemoFullName,
            Phone = DemoSettings.DemoPhone,
            Password = DemoSettings.DemoPassword,
            IsPhoneVerified = true,
            IsDemoAccount = true,
        });
    }

    public UserAccount? CurrentUser { get; private set; }
    public bool IsSignedIn => CurrentUser is not null;
    public event EventHandler? SessionChanged;

    public async Task<AuthResult> SignInAsync(string phone, string password)
    {
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        var normalized = PhoneNumber.Normalize(phone);
        var account = _accounts.FirstOrDefault(a => a.Phone == normalized);
        if (account is null || account.Password != password)
        {
            return AuthResult.Fail(AuthFailure.InvalidCredentials);
        }
        CurrentUser = account;
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return AuthResult.Ok;
    }

    public async Task<AuthResult> RegisterAsync(RegistrationRequest request)
    {
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        var normalized = PhoneNumber.Normalize(request.Phone);
        if (_accounts.Any(a => a.Phone == normalized))
        {
            return AuthResult.Fail(AuthFailure.PhoneAlreadyUsed);
        }
        var account = new UserAccount
        {
            FullName = request.FullName.Trim(),
            Phone = normalized,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Password = request.Password,
            IsPhoneVerified = false,
        };
        _accounts.Add(account);
        CurrentUser = account;
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return AuthResult.Ok;
    }

    public Task SignOutAsync()
    {
        CurrentUser = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task<AuthResult> RequestPasswordResetAsync(string phone)
    {
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        // SIMULATION : succès systématique, qu'un compte existe ou non (pas d'énumération de comptes).
        return AuthResult.Ok;
    }

    public async Task<AuthResult> VerifyResetCodeAsync(string phone, string code)
    {
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        var normalized = PhoneNumber.Normalize(phone);
        var exists = _accounts.Any(a => a.Phone == normalized);
        if (!exists || code.Trim() != DemoSettings.ResetCode)
        {
            return AuthResult.Fail(AuthFailure.InvalidCode);
        }
        _verifiedResets.Add(normalized);
        return AuthResult.Ok;
    }

    public async Task<AuthResult> ResetPasswordAsync(string phone, string newPassword)
    {
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        var normalized = PhoneNumber.Normalize(phone);
        var account = _accounts.FirstOrDefault(a => a.Phone == normalized);
        if (account is null || !_verifiedResets.Remove(normalized))
        {
            return AuthResult.Fail(AuthFailure.NotVerified);
        }
        account.Password = newPassword;
        return AuthResult.Ok;
    }
}
