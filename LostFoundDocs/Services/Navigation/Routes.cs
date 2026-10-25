namespace LostFoundDocs.Services.Navigation;

public static class Routes
{
    // Onglets et écrans racine
    public const string Splash = "splash";
    public const string Login = "login";
    public const string Register = "register";
    public const string ForgotPassword = "forgotpassword";
    public const string Home = "home";
    public const string Reports = "reports";
    public const string Messages = "messages";
    public const string Profile = "profile";

    // Déclarations
    public const string DeclareLost = "declare-lost";
    public const string DeclareFound = "declare-found";
    public const string DeclarationConfirmation = "declaration-confirmation";

    // Recherche et correspondances
    public const string Search = "search";
    public const string MatchResult = "match-result";
    public const string NoMatch = "no-match";
    public const string MatchDetails = "match-details";

    // Signalements, messages, notifications
    public const string ReportDetails = "report-details";
    public const string Conversation = "conversation";
    public const string Notifications = "notifications";

    // Restitution
    public const string Restitution = "restitution";
    public const string Restituted = "restituted";
    public const string Receipt = "receipt";

    // Profil
    public const string PersonalInfo = "personal-info";
    public const string Security = "security";
    public const string NotificationPrefs = "notification-prefs";
    public const string Privacy = "privacy";
    public const string Help = "help";
    public const string About = "about";

    private static readonly HashSet<string> Available = new()
    {
        Splash, Login, Register, ForgotPassword, Home, Reports, Messages, Profile,
    };

    /// <summary>Déclare une route comme disponible (appelé quand l'écran correspondant est livré).</summary>
    public static void MarkAvailable(params string[] routes)
    {
        foreach (var r in routes) Available.Add(r);
    }

    public static bool IsAvailable(string route)
    {
        var key = route.Split('?')[0].TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
        return key == ".." || Available.Contains(key);
    }
}
