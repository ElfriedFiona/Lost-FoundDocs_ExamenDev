namespace LostFoundDocs.Models;

public enum MatchState { Match, Partial, NotCompared }

public sealed record MatchCriterion(string Name, MatchState State);

/// <summary>Correspondance détectée pour un signalement de l'utilisateur (SIMULATION : calculée localement).</summary>
public sealed class MatchResult : ObservableObject
{
    private bool _isDisputed;
    private string? _disputeReason;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ReportId { get; init; }
    public ReportKind UserSide { get; init; }
    public string CounterpartReference { get; init; } = string.Empty;
    public string CounterpartName { get; init; } = string.Empty;
    public DocumentType Type { get; init; }
    public string City { get; init; } = string.Empty;
    /// <summary>Date de découverte (si l'utilisateur a perdu) ou date de perte déclarée (si l'utilisateur a trouvé).</summary>
    public DateTime CounterpartDate { get; init; }
    public int Score { get; init; }
    public IReadOnlyList<MatchCriterion> Criteria { get; init; } = Array.Empty<MatchCriterion>();
    public DateTime FoundAt { get; init; }

    public bool IsDisputed { get => _isDisputed; set => SetProperty(ref _isDisputed, value); }
    public string? DisputeReason { get => _disputeReason; set => SetProperty(ref _disputeReason, value); }

    public string Strength => Score >= 85 ? "Correspondance forte" : "Correspondance probable";
}

public sealed record MatchOutcome(bool Found, MatchResult? Match);
