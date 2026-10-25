using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Models;

public sealed record MeetingPlace(string Name, string District);

public sealed class RestitutionProposal
{
    public Guid ReportId { get; init; }
    public string Place { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public DateTime Slot { get; init; }
    /// <summary>Vrai si l'utilisateur a fait la proposition ; faux si elle vient de l'autre personne (simulée).</summary>
    public bool ProposedByUser { get; init; }
    public bool Accepted { get; set; }

    public string SlotLabel => $"{DateFormat.Long(Slot)} à {DateFormat.Time(Slot)}";
}

public sealed class Receipt
{
    public Guid ReportId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string DocumentTitle { get; init; } = string.Empty;
    public DateTime ReturnedAt { get; init; }
    public string City { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string HandedByName { get; init; } = string.Empty;
    public int Score { get; init; }
    public string VerificationCode { get; init; } = string.Empty;

    public string DateLabel => $"{DateFormat.Short(ReturnedAt)} à {DateFormat.Time(ReturnedAt)}";
    public string MatchLabel => $"{Score} % · automatique";

    /// <summary>Code de démonstration dérivé de la référence (SIMULATION : non vérifiable).</summary>
    public static string BuildCode(string reference)
    {
        var digits = new string(reference.Where(char.IsDigit).ToArray());
        var last4 = digits.Length >= 4 ? digits[^4..] : digits.PadLeft(4, '0');
        var hash = reference.Aggregate(17, (h, c) => unchecked(h * 31 + c));
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var tail = new string(Enumerable.Range(0, 4).Select(i => alphabet[Math.Abs((hash >> (i * 5)) % alphabet.Length)]).ToArray());
        return $"{last4}-{tail}";
    }
}
