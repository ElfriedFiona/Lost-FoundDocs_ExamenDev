namespace LostFoundDocs.Services.Navigation;

public static class Routes
{
    // Disponibles dès la phase A
    public const string Splash = "splash";
    public const string Login = "login";
    public const string Register = "register";
    public const string ForgotPassword = "forgotpassword";
    public const string Home = "home";
    public const string Reports = "reports";
    public const string Messages = "messages";
    public const string Profile = "profile";

    // Prévues en phase B (tant qu'elles ne sont pas déclarées, la navigation affiche « Bientôt disponible »)
    public const string DeclareLost = "declare-lost";
    public const string DeclareFound = "declare-found";
    public const string Notifications = "notifications";
    public const string ReportDetails = "report-details";

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
