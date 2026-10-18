using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Abstractions;

public enum AuthFailure { None, InvalidCredentials, PhoneAlreadyUsed, InvalidCode, NotVerified }

public sealed record AuthResult(bool Success, AuthFailure Failure = AuthFailure.None)
{
    public static AuthResult Ok { get; } = new(true);
    public static AuthResult Fail(AuthFailure failure) => new(false, failure);
}

public sealed record RegistrationRequest(string FullName, string Phone, string? Email, string Password);

public interface IAuthService
{
    UserAccount? CurrentUser { get; }
    bool IsSignedIn { get; }
    event EventHandler? SessionChanged;

    Task<AuthResult> SignInAsync(string phone, string password);
    Task<AuthResult> RegisterAsync(RegistrationRequest request);
    Task SignOutAsync();

    Task<AuthResult> RequestPasswordResetAsync(string phone);
    Task<AuthResult> VerifyResetCodeAsync(string phone, string code);
    Task<AuthResult> ResetPasswordAsync(string phone, string newPassword);
}
