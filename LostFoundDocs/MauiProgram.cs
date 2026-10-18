using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.ViewModels;
using LostFoundDocs.Views.Auth;
using LostFoundDocs.Views.Home;
using LostFoundDocs.Views.Shared;
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
        RegisterServices(builder.Services);
        RegisterViewModelsAndPages(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // SIMULATION (frontend uniquement) : implémentations en mémoire.
        // Phase backend : remplacer les services fictifs par des implémentations HTTP.
        services.AddSingleton<IAuthService, MockAuthService>();
        services.AddSingleton<IReportService, MockReportService>();
        services.AddSingleton<INotificationService, MockNotificationService>();
        services.AddSingleton<IMatchService, MockMatchService>();
        services.AddSingleton<IConversationService, MockConversationService>();
        services.AddSingleton<IRestitutionService, MockRestitutionService>();
        services.AddSingleton<IProfileService, MockProfileService>();

        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService, ShellNavigationService>();
    }

    private static void RegisterViewModelsAndPages(IServiceCollection services)
    {
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<ForgotPasswordViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<DeclarationViewModel>();
        services.AddTransient<ReportDetailsViewModel>();
        services.AddTransient<MatchViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<MessagesViewModel>();
        services.AddTransient<ConversationViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<RestitutionViewModel>();
        services.AddTransient<ReceiptViewModel>();
        services.AddTransient<ProfileSettingsViewModel>();

        services.AddTransient<SplashPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<ForgotPasswordPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<ReportsPage>();
        services.AddTransient<MessagesPage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<Views.Declaration.DeclarationPage>();
        services.AddTransient<Views.Declaration.ConfirmationPage>();
        services.AddTransient<Views.Reports.ReportDetailsPage>();
        services.AddTransient<Views.Matching.MatchPage>();
        services.AddTransient<Views.Matching.MatchDetailsPage>();
        services.AddTransient<Views.Messages.ConversationPage>();
        services.AddTransient<Views.Notifications.NotificationsPage>();
        services.AddTransient<Views.Restitution.RestitutionPage>();
        services.AddTransient<Views.Restitution.ReturnedPage>();
        services.AddTransient<Views.Restitution.ReceiptPage>();
        services.AddTransient<Views.Profile.PersonalPage>();
        services.AddTransient<Views.Profile.SecurityPage>();
        services.AddTransient<Views.Profile.PreferencesPage>();
        services.AddTransient<Views.Profile.PrivacyPage>();
        services.AddTransient<Views.Profile.HelpPage>();
        services.AddTransient<Views.Profile.AboutPage>();
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
