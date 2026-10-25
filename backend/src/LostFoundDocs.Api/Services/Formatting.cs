namespace LostFoundDocs.Api.Services;

/// <summary>Formats français indépendants de la culture du serveur (les conteneurs n'ont pas toujours les données de localisation).</summary>
public static class Formatting
{
    private static readonly string[] Months =
        { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre" };

    public static DateTime ToLocal(DateTime utc, int offsetHours) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(offsetHours);

    public static string LongDate(DateTime local) => $"{local.Day} {Months[local.Month - 1]} {local.Year}";

    public static string LocalDateTime(DateTime utc, int offsetHours)
    {
        var l = ToLocal(utc, offsetHours);
        return $"{l:dd/MM/yyyy} à {l:HH:mm}";
    }
}
