using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Navigation;

public sealed class ShellNavigationService : INavigationService
{
    private readonly IDialogService _dialogs;

    public ShellNavigationService(IDialogService dialogs) => _dialogs = dialogs;

    public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        // Échafaudage de la phase A : les écrans de la phase B ne sont pas encore déclarés.
        if (!Routes.IsAvailable(route))
        {
            await _dialogs.AlertAsync("Bientôt disponible", "Cet écran sera livré dans une prochaine phase du frontend.");
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (parameters is null)
                await Shell.Current.GoToAsync(route);
            else
                await Shell.Current.GoToAsync(route, parameters);
        });
    }

    public Task GoBackAsync() => MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));
}
