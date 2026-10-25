using System.Globalization;

namespace LostFoundDocs.Services.Validation;

public static class NameFormat
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>« aicha mbarga » → « Aicha Mbarga » (casse uniquement ; les accents ne sont jamais devinés).</summary>
    public static string Capitalize(string name) =>
        string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Length == 1 ? p.ToUpper(Fr) : char.ToUpper(p[0], Fr) + p[1..].ToLower(Fr)));

    /// <summary>« Aïcha Mbarga » → « Aïcha M. » (nom affiché à l'autre partie, par confidentialité).</summary>
    public static string Short(string fullName)
    {
        var parts = Capitalize(fullName).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length < 2 ? (parts.FirstOrDefault() ?? string.Empty) : $"{parts[0]} {parts[^1][0]}.";
    }
}
