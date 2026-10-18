using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Models;

/// <summary>Signalement enregistré (perte ou trouvaille). Créé une seule fois ; seul son statut évolue.</summary>
public sealed class Report : ObservableObject
{
    private ReportStatus _status = ReportStatus.Searching;
    private DateTime _updatedAt;

    public List<ReportStatusChange> History { get; } = new();
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Reference { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public ReportKind Kind { get; init; }
    public DocumentType Type { get; init; }
    public string? CustomTitle { get; init; }
    public string HolderName { get; init; } = string.Empty;
    public string? Number { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Issuer { get; init; }
    public DateTime EventDate { get; init; }
    public string City { get; init; } = string.Empty;
    public string Place { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? PhotoPath { get; init; }
    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    public ReportStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsTerminal));
            }
        }
    }

    public string Title => Type == DocumentType.Other && !string.IsNullOrWhiteSpace(CustomTitle)
        ? CustomTitle!
        : DocumentTypes.Get(Type).Label;

    public string DateLabel => $"{DocumentTypes.EventVerb(Type, Kind)} · {DateFormat.Short(EventDate)}";
    public string DateLabelLong => $"{DocumentTypes.EventVerb(Type, Kind)} · {DateFormat.Long(EventDate)}";
    public string LocationLabel => string.IsNullOrWhiteSpace(Place) ? City : $"{City} · {Place}";
    public string MaskedNumber => Masking.MaskNumber(Number);
    public string StatusLabel => ReportStatusRules.Label(Status);
    public bool IsTerminal => ReportStatusRules.IsTerminal(Status);
}

public sealed record ReportStatusChange(ReportStatus Status, DateTime ChangedAt);
