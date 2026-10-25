using System.Text.RegularExpressions;

namespace LostFoundDocs.Services.Validation;

/// <summary>Règles de validation pures (sans MAUI) : renvoient un message d'erreur, ou null si la valeur est valide.</summary>
public static class FieldRules
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex LongNumberRegex = new(@"\d{8,}", RegexOptions.Compiled);

    public static string? FullName(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length == 0) return "Le nom complet est obligatoire.";
        if (v.Length < 2 || !v.Any(char.IsLetter)) return "Saisissez au moins 2 caractères (lettres).";
        return null;
    }

    public static string? Phone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Le numéro de téléphone est obligatoire.";
        return PhoneNumber.IsValid(value) ? null : "Saisissez un numéro à 9 chiffres commençant par 6.";
    }

    public static string? PasswordRequired(string? value) =>
        string.IsNullOrEmpty(value) ? "Le mot de passe est obligatoire." : null;

    public static string? NewPassword(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "Le mot de passe est obligatoire.";
        return value.Length < 8 ? "8 caractères minimum." : null;
    }

    public static string? PasswordConfirmation(string? password, string? confirmation)
    {
        if (string.IsNullOrEmpty(confirmation)) return "Confirmez le mot de passe.";
        return password == confirmation ? null : "Les mots de passe ne correspondent pas.";
    }

    /// <summary>E-mail facultatif : vide accepté, sinon format valide.</summary>
    public static string? OptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return EmailRegex.IsMatch(value.Trim()) ? null : "Adresse e-mail invalide.";
    }

    /// <summary>Date de naissance FACULTATIVE : null accepté ; sinon passée et plausible (≤ 120 ans).</summary>
    public static string? OptionalBirthDate(DateTime? value, DateTime today)
    {
        if (value is null) return null;
        if (value.Value.Date > today.Date) return "La date de naissance ne peut pas être dans le futur.";
        if (value.Value.Date < today.Date.AddYears(-120)) return "Date de naissance invalide.";
        return null;
    }

    /// <summary>Date d'un événement (perte, découverte) : obligatoire, ≤ aujourd'hui, et pas avant la naissance si elle est connue.</summary>
    public static string? EventDate(DateTime? value, DateTime today, DateTime? birthDate, string requiredMessage)
    {
        if (value is null) return requiredMessage;
        if (value.Value.Date > today.Date) return "La date ne peut pas être dans le futur.";
        if (birthDate is not null && value.Value.Date < birthDate.Value.Date)
            return "La date ne peut pas précéder la date de naissance.";
        return null;
    }

    /// <summary>Numéro de document FACULTATIF : vide accepté ; sinon 4 à 20 lettres/chiffres (espaces et tirets tolérés).</summary>
    public static string? OptionalDocumentNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var compact = new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '/').ToArray());
        var ok = compact.Length is >= 4 and <= 20 && compact.All(char.IsLetterOrDigit);
        return ok ? null : "Le numéro doit contenir 4 à 20 caractères (lettres et chiffres).";
    }

    public static string? Notes(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > 300) return "300 caractères maximum.";
        var compact = new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
        return LongNumberRegex.IsMatch(compact) ? "N’indiquez aucun numéro de document complet ici." : null;
    }

    public static string? Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? message : null;
}
