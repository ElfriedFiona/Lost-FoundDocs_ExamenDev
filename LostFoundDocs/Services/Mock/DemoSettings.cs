namespace LostFoundDocs.Services.Mock;

public enum MatchMode { Auto, AlwaysMatch, NeverMatch }

/// <summary>
/// SIMULATION (frontend uniquement) : paramètres du mode démonstration.
/// Rien de ce qui est défini ici n'existera tel quel dans la version avec backend.
/// </summary>
public static class DemoSettings
{
    public const string DemoFullName = "Aïcha Mbarga";
    public const string DemoPhone = "677124821";          // affiché : +237 6 77 12 48 21
    public const string DemoPhoneDisplay = "6 77 12 48 21";
    public const string DemoPassword = "Demo@2026";
    public const string ResetCode = "123456";             // code SMS fictif (aucun SMS n'est envoyé)

    public static int SimulatedLatencyMs { get; set; } = 500;
    public static MatchMode MatchMode { get; set; } = MatchMode.Auto;   // utilisé en phase B
}
