using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Navigation;

/// <summary>Version phase A : boîtes de dialogue natives. Les modales stylées de la maquette arrivent en phase B.</summary>
public sealed class DialogService : IDialogService
{
    public Task AlertAsync(string title, string message, string okText = "OK") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(title, message, okText));

    public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText = "Annuler") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(title, message, confirmText, cancelText));
}
