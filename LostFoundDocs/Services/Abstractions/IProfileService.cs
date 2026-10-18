namespace LostFoundDocs.Services.Abstractions;
public interface IProfileService
{
    bool[] NotificationPreferences { get; }
    int MatchModeIndex { get; set; }
    Task SaveInformationAsync(string name, string email);
    Task ChangePasswordAsync(string current, string password, string confirmation);
    Task ResetDemoAsync(bool populated);
}
