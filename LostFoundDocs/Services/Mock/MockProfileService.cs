using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;
namespace LostFoundDocs.Services.Mock;

// SIMULATION (frontend uniquement) : préférences en mémoire et aucune authentification réelle.
public sealed class MockProfileService(IAuthService auth, IReportService reports) : IProfileService
{
    private readonly Dictionary<Guid, bool[]> _preferences = new();
    public bool[] NotificationPreferences
    {
        get
        {
            var id = auth.CurrentUser?.Id ?? throw new InvalidOperationException("Session expirée.");
            if (!_preferences.TryGetValue(id, out var values)) _preferences[id] = values = [true, true, true, true];
            return values;
        }
    }
    public int MatchModeIndex
    {
        get => (int)DemoSettings.MatchMode;
        set
        {
            if (value is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(value));
            DemoSettings.MatchMode = (MatchMode)value;
        }
    }
    public Task SaveInformationAsync(string name, string email)
    {
        var error = FieldRules.FullName(name) ?? FieldRules.OptionalEmail(email);
        if (error is not null) throw new ArgumentException(error);
        var user = auth.CurrentUser ?? throw new InvalidOperationException("Session expirée.");
        user.FullName = name.Trim(); user.Email = email.Trim();
        return Task.CompletedTask;
    }
    public Task ChangePasswordAsync(string current, string password, string confirmation)
    {
        var user = auth.CurrentUser ?? throw new InvalidOperationException("Session expirée.");
        if (user.Password != current) throw new ArgumentException("Mot de passe actuel incorrect.");
        var error = FieldRules.NewPassword(password) ?? FieldRules.PasswordConfirmation(password, confirmation);
        if (error is not null) throw new ArgumentException(error);
        user.Password = password;
        return Task.CompletedTask;
    }
    public Task ResetDemoAsync(bool populated) => reports.ResetAsync(populated);
}
