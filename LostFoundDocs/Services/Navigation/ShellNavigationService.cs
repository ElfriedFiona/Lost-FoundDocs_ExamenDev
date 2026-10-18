using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
namespace LostFoundDocs.Services.Navigation;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (route is Routes.DeclareLost or Routes.DeclareFound)
            parameters = new Dictionary<string, object> { ["kind"] = route == Routes.DeclareFound ? ReportKind.Found : ReportKind.Lost };
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (parameters is null) await Shell.Current.GoToAsync(route);
            else await Shell.Current.GoToAsync(route, new ShellNavigationQueryParameters(parameters));
        });
    }
    public Task GoBackAsync() => MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));
}
