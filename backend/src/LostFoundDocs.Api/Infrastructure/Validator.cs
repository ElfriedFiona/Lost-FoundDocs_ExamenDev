using System.Text.RegularExpressions;

namespace LostFoundDocs.Api.Infrastructure;

/// <summary>Accumule les erreurs de validation par champ puis lève une erreur 400 unique.</summary>
public sealed class Validator
{
    private static readonly Regex Email = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex LongNumber = new(@"\d{8,}", RegexOptions.Compiled);
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool IsValid => _errors.Count == 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list)) _errors[field] = list = new List<string>();
        list.Add(message);
    }

    public void ThrowIfInvalid()
    {
        if (IsValid) return;
        throw ApiException.BadRequest("validation_failed", "Certaines informations sont invalides.",
            _errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
    }

    public void FullName(string field, string? value)
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) Add(field, "Le nom complet est obligatoire.");
        else if (v.Length < 2 || !v.Any(char.IsLetter)) Add(field, "Saisissez au moins 2 caractères (lettres).");
        else if (v.Length > 120) Add(field, "120 caractères maximum.");
    }

    public void Phone(string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) Add(field, "Le numéro de téléphone est obligatoire.");
        else if (!PhoneNumber.IsValid(value)) Add(field, "Saisissez un numéro à 9 chiffres commençant par 6.");
    }

    public void NewPassword(string field, string? value)
    {
        if (string.IsNullOrEmpty(value)) Add(field, "Le mot de passe est obligatoire.");
        else if (value.Length < 8) Add(field, "8 caractères minimum.");
        else if (value.Length > 128) Add(field, "128 caractères maximum.");
    }

    public void OptionalEmail(string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (value.Length > 200 || !Email.IsMatch(value.Trim())) Add(field, "Adresse e-mail invalide.");
    }

    public void OptionalDocumentNumber(string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var compact = new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '/').ToArray());
        if (compact.Length is < 4 or > 20 || !compact.All(char.IsLetterOrDigit))
            Add(field, "Le numéro doit contenir 4 à 20 caractères (lettres et chiffres).");
    }

    public void Notes(string field, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (value.Length > 300) { Add(field, "300 caractères maximum."); return; }
        var compact = new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
        if (LongNumber.IsMatch(compact)) Add(field, "N’indiquez aucun numéro de document complet ici.");
    }

    public void Required(string field, string? value, string message, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value)) Add(field, message);
        else if (value.Trim().Length > maxLength) Add(field, $"{maxLength} caractères maximum.");
    }
}

public static class PhoneNumber
{
    public static string Normalize(string? input)
    {
        var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00237") && digits.Length == 14) return digits[5..];
        if (digits.StartsWith("237") && digits.Length == 12) return digits[3..];
        return digits;
    }

    public static bool IsValid(string? input)
    {
        var n = Normalize(input);
        return n.Length == 9 && n[0] == '6';
    }
}
