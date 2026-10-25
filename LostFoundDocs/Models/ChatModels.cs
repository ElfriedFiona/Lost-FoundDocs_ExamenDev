using System.Collections.ObjectModel;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Models;

public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; init; } = string.Empty;
    public bool IsMine { get; init; }
    public bool IsSystem { get; init; }
    public bool IsSimulatedReply { get; init; }
    public DateTime SentAt { get; init; }
    public string TimeLabel => DateFormat.Time(SentAt);
}

public sealed class Conversation : ObservableObject
{
    private int _unreadCount;
    private bool _isClosed;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ReportId { get; init; }
    public string DocumentTitle { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    /// <summary>Vrai si l'utilisateur est le propriétaire du document (l'autre personne l'a trouvé).</summary>
    public bool UserIsOwner { get; init; }
    public string CounterpartName { get; init; } = string.Empty;
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    /// <summary>Vrai tant que l'écran de conversation est ouvert (une réponse reçue n'est alors pas « non lue »).</summary>
    public bool IsViewed { get; set; }

    public int UnreadCount { get => _unreadCount; set { if (SetProperty(ref _unreadCount, value)) OnPropertyChanged(nameof(HasUnread)); } }
    public bool IsClosed { get => _isClosed; set => SetProperty(ref _isClosed, value); }
    public bool HasUnread => UnreadCount > 0;

    public DateTime LastAt => Messages.Count == 0 ? DateTime.MinValue : Messages[^1].SentAt;
    public string TimeLabel => LastAt.Date == Services.Clock.Today ? DateFormat.Time(LastAt) : DateFormat.Short(LastAt);

    public string Preview
    {
        get
        {
            if (Messages.Count == 0) return string.Empty;
            var last = Messages[^1];
            return last.IsSystem ? last.Text : last.IsMine ? $"Vous : {last.Text}" : last.Text;
        }
    }

    public string Subtitle => UserIsOwner ? "Correspondance avec un document trouvé" : "Correspondance avec un signalement de perte";
    public string RoleLabel => UserIsOwner ? "a trouvé votre document" : "propriétaire";

    public void Add(ChatMessage message)
    {
        Messages.Add(message);
        OnPropertyChanged(nameof(Preview));
        OnPropertyChanged(nameof(LastAt));
        OnPropertyChanged(nameof(TimeLabel));
    }
}
