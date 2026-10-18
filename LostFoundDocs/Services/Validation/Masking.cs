namespace LostFoundDocs.Services.Validation;

public static class Masking
{
    /// <summary>« Masqué partout dans l'application » : seuls les 4 derniers caractères sont affichés.</summary>
    public static string MaskNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return string.Empty;
        var compact = new string(number.Where(char.IsLetterOrDigit).ToArray());
        if (compact.Length <= 4) return new string('•', compact.Length);
        return $"•••• {compact[^4..]}";
    }
}
