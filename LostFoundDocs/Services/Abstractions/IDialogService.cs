namespace LostFoundDocs.Services.Abstractions;

public interface IDialogService
{
    Task AlertAsync(string title, string message, string okText = "OK");
    Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText = "Annuler");
}
