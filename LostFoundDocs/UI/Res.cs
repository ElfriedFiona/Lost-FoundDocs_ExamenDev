namespace LostFoundDocs.UI;

/// <summary>Accès aux couleurs et brushes déclarés dans Resources/Styles/Colors.xaml depuis le code.</summary>
public static class Res
{
    public static Color GetColor(string key) => Lookup(key, Colors.Magenta);

    public static Brush GetBrush(string key) => Lookup<Brush>(key, new SolidColorBrush(Colors.Magenta));

    private static T Lookup<T>(string key, T fallback)
    {
        var resources = Application.Current?.Resources;
        if (resources is not null && resources.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }
        return fallback;
    }
}
