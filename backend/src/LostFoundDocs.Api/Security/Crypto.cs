using System.Security.Cryptography;
using System.Text;

namespace LostFoundDocs.Api.Security;

/// <summary>Mots de passe : PBKDF2-HMAC-SHA256 avec sel aléatoire. Jetons : aléatoires, stockés sous forme de hachage SHA-256.</summary>
public static class PasswordHasher
{
    public static string Hash(string password, int iterations)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored, out bool needsRehash, int currentIterations)
    {
        needsRehash = false;
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
            needsRehash = ok && iterations < currentIterations;
            return ok;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Vérification factice pour garder un temps de réponse comparable quand le compte n'existe pas.</summary>
    public static void BurnTime(string password, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, new byte[16], iterations, HashAlgorithmName.SHA256, 32);
}

public static class Tokens
{
    public static string Generate() => ToBase64Url(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public static string ToBase64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Code numérique à 6 chiffres, tirage uniforme.</summary>
    public static string SixDigitCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    /// <summary>Code de vérification de reçu (ex. K7QD-4M2X), alphabet sans caractères ambigus.</summary>
    public static string VerificationCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[9];
        for (var i = 0; i < 9; i++)
            chars[i] = i == 4 ? '-' : alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return new string(chars);
    }
}

public sealed class Fingerprinter
{
    private readonly byte[] _key;

    public Fingerprinter(string secret) => _key = Encoding.UTF8.GetBytes(secret);

    /// <summary>Empreinte HMAC-SHA256 tronquée à 32 caractères hexadécimaux, par domaine (numéro, naissance, code).</summary>
    public string Hash(string purpose, string value)
    {
        var data = Encoding.UTF8.GetBytes(purpose + "|" + value);
        return Convert.ToHexString(HMACSHA256.HashData(_key, data))[..32].ToLowerInvariant();
    }

    public string Number(string number) => Hash("number", new string(number.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant());
    public string BirthDate(DateTime date) => Hash("birth", date.ToString("yyyy-MM-dd"));
    public string Otp(string phone, string code) => Hash("otp:" + phone, code);
}
