using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Navigation;

/// <summary>Boîtes de dialogue natives (confirmations, feuilles d'actions). Les modales à choix multiples stylées sont des composants dédiés.</summary>
public sealed class DialogService : IDialogService
{
    public Task AlertAsync(string title, string message, string okText = "OK") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(title, message, okText));

    public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText = "Annuler") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(title, message, confirmText, cancelText));

    public Task<string?> ChooseAsync(string title, string cancelText, params string[] options) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var choice = await Shell.Current.DisplayActionSheet(title, cancelText, null, options);
            return string.IsNullOrEmpty(choice) || choice == cancelText ? null : choice;
        });
}
