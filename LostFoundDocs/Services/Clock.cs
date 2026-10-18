namespace LostFoundDocs.Services;

/// <summary>Horloge unique de l'application : toutes les dates fictives en dérivent (cohérence chronologique, tests).</summary>
public static class Clock
{
    public static Func<DateTime> Provider { get; set; } = () => DateTime.Now;
    public static DateTime Now => Provider();
    public static DateTime Today => Provider().Date;
}
