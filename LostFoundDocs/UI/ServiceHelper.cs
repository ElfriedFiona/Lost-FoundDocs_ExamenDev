using Microsoft.Extensions.DependencyInjection;

namespace LostFoundDocs.UI;

/// <summary>Accès au conteneur d'injection depuis les composants visuels (contrôles sans constructeur injectable).</summary>
public static class ServiceHelper
{
    public static T Get<T>() where T : notnull =>
        IPlatformApplication.Current!.Services.GetRequiredService<T>();
}
