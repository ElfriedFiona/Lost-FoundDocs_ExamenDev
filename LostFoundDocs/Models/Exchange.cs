using System.Collections.ObjectModel;
namespace LostFoundDocs.Models;

public sealed record MatchResult(Guid ReportId, int Score, string Contact, DateTime EventDate)
{
    public string ScoreLabel => $"{Score} % · Correspondance forte";
    public double Progress => Score / 100d;
}

public sealed record ChatMessage(string Author, string Text, DateTime SentAt)
{
    public string Caption => $"{Author} · {SentAt:HH:mm}";
}

public sealed class Conversation : ObservableObject
{
    private int _unread;
    public required Report Report { get; init; }
    public required string Contact { get; init; }
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public int Unread { get => _unread; set { SetProperty(ref _unread, value); OnPropertyChanged(nameof(UnreadLabel)); } }
    public string UnreadLabel => Unread == 0 ? "Lu" : $"{Unread} non lu(s)";
    public string Preview => Messages.LastOrDefault()?.Text ?? "Commencez la conversation";
    public string Role => Report.Kind == ReportKind.Lost ? "A trouvé le document" : "Propriétaire";
    public void Add(ChatMessage message) { Messages.Add(message); OnPropertyChanged(nameof(Preview)); }
}

public sealed class AppNotification : ObservableObject
{
    private bool _isRead;
    public Guid Id { get; } = Guid.NewGuid();
    public required Guid ReportId { get; init; }
    public required string Title { get; init; }
    public required string Route { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsRead { get => _isRead; set { SetProperty(ref _isRead, value); OnPropertyChanged(nameof(State)); } }
    public string State => IsRead ? "Lue" : "Non lue";
}

public sealed record RestitutionProposal(string Place, DateTime Appointment, bool Accepted)
{
    public string Summary => $"{Place}\n{Appointment:dd/MM/yyyy à HH:mm}";
}
public sealed record Receipt(Report Report, string Owner, string Finder, DateTime ReturnedAt, int Score)
{
    public bool Saved { get; set; }
    public string Summary => $"{Report.Title} · {Report.Reference}\n{Report.City} · {ReturnedAt:dd/MM/yyyy HH:mm}\nPropriétaire : {Owner}\nRemis par : {Finder}\nCorrespondance : {Score} %";
}
