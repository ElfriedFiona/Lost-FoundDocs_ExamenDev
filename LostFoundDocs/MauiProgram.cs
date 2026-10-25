using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace LostFoundDocs;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("lucide.ttf", "Lucide"); // police d'icônes (voir THIRD_PARTY_NOTICES.md)
            });

        ConfigureBorderlessEntry();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    /// <summary>
    /// Les champs de saisie dessinent leur propre cadre (composant FormField) :
    /// on retire donc le trait natif sous les Entry sur Android et Windows.
    /// </summary>
    private static void ConfigureBorderlessEntry()
    {
        EntryHandler.Mapper.AppendToMapping("BorderlessEntry", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var textBox = handler.PlatformView;
            textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            textBox.Background = transparent;
            textBox.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(0);
            textBox.Resources["TextControlBackgroundFocused"] = transparent;
            textBox.Resources["TextControlBackgroundPointerOver"] = transparent;
#endif
        });
    }
}
