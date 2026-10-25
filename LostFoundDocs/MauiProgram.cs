using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.ViewModels;
using LostFoundDocs.Views.Auth;
using LostFoundDocs.Views.Home;
using LostFoundDocs.Views.Shared;
using LostFoundDocs.Views.Declaration;
using LostFoundDocs.Views.Matching;
using LostFoundDocs.Views.Messages;
using LostFoundDocs.Views.Profile;
using LostFoundDocs.Views.Reports;
using LostFoundDocs.Views.Restitution;
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

        ConfigureNativeFields();
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
        // Phase backend : remplacer ces enregistrements par des implémentations HTTP des mêmes interfaces.
        services.AddSingleton<IAuthService, MockAuthService>();
        services.AddSingleton<MockReportService>();
        services.AddSingleton<IReportService>(sp => sp.GetRequiredService<MockReportService>());
        services.AddSingleton<IResettable>(sp => sp.GetRequiredService<MockReportService>());
        services.AddSingleton<DemoWorld>();
        services.AddSingleton<IResettable>(sp => sp.GetRequiredService<DemoWorld>());
        services.AddSingleton<IConversationService, MockConversationService>();
        services.AddSingleton<INotificationService, MockNotificationService>();
        services.AddSingleton<IMatchService, MockMatchService>();
        services.AddSingleton<IRestitutionService, MockRestitutionService>();

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
        services.AddTransient<DeclareLostViewModel>();
        services.AddTransient<DeclareFoundViewModel>();
        services.AddTransient<DeclarationConfirmationViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<MatchResultViewModel>();
        services.AddTransient<NoMatchViewModel>();
        services.AddTransient<MatchDetailsViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReportDetailsViewModel>();
        services.AddTransient<MessagesViewModel>();
        services.AddTransient<ConversationViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<RestitutionViewModel>();
        services.AddTransient<RestitutedViewModel>();
        services.AddTransient<ReceiptViewModel>();
        services.AddTransient<PersonalInfoViewModel>();
        services.AddTransient<SecurityViewModel>();
        services.AddTransient<NotificationPrefsViewModel>();
        services.AddTransient<HelpViewModel>();
        services.AddTransient<AboutViewModel>();

        services.AddTransient<SplashPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<ForgotPasswordPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<ReportsPage>();
        services.AddTransient<MessagesPage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<DeclareLostPage>();
        services.AddTransient<DeclareFoundPage>();
        services.AddTransient<DeclarationConfirmationPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<MatchResultPage>();
        services.AddTransient<NoMatchPage>();
        services.AddTransient<MatchDetailsPage>();
        services.AddTransient<ReportDetailsPage>();
        services.AddTransient<ConversationPage>();
        services.AddTransient<NotificationsPage>();
        services.AddTransient<RestitutionPage>();
        services.AddTransient<RestitutedPage>();
        services.AddTransient<ReceiptPage>();
        services.AddTransient<PersonalInfoPage>();
        services.AddTransient<SecurityPage>();
        services.AddTransient<NotificationPrefsPage>();
        services.AddTransient<PrivacyPage>();
        services.AddTransient<HelpPage>();
        services.AddTransient<AboutPage>();
    }

    /// <summary>
    /// Les champs dessinent leur propre cadre (composants FormField, SelectField, DateField) :
    /// on retire donc le trait natif sous les Entry et les Picker sur Android et Windows.
    /// </summary>
    private static void ConfigureNativeFields()
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

        PickerHandler.Mapper.AppendToMapping("BorderlessPicker", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
        });
    }
}
