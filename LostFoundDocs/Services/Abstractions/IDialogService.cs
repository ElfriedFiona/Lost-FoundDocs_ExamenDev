namespace LostFoundDocs.Services.Abstractions;

public interface IDialogService
{
    Task<string?> ChooseAsync(string title, string cancelText, params string[] choices);
    Task AlertAsync(string title, string message, string okText = "OK");
    Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText = "Annuler");
}
