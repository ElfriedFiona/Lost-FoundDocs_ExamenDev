namespace LostFoundDocs.Services.Validation;

public static class PhoneNumber
{
    /// <summary>Ne garde que les chiffres et retire le préfixe pays (+237, 00237, 237).</summary>
    public static string Normalize(string? input)
    {
        var digits = new string((input ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00237") && digits.Length == 14) return digits[5..];
        if (digits.StartsWith("237") && digits.Length == 12) return digits[3..];
        return digits;
    }

    /// <summary>Numéro mobile camerounais : 9 chiffres commençant par 6.</summary>
    public static bool IsValid(string? input)
    {
        var n = Normalize(input);
        return n.Length == 9 && n[0] == '6';
    }

    public static string Format(string nineDigits) =>
        nineDigits.Length == 9
            ? $"{nineDigits[0]} {nineDigits[1..3]} {nineDigits[3..5]} {nineDigits[5..7]} {nineDigits[7..9]}"
            : nineDigits;
}
