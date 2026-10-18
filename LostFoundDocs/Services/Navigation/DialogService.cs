using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Navigation;

/// <summary>Dialogues natifs de confirmation et de choix, accessibles sur Android et Windows.</summary>
public sealed class DialogService : IDialogService
{
    public async Task<string?> ChooseAsync(string title, string cancelText, params string[] choices) =>
        await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayActionSheet(title, cancelText, null, choices));

    public Task AlertAsync(string title, string message, string okText = "OK") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(title, message, okText));

    public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText = "Annuler") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(title, message, confirmText, cancelText));
}
