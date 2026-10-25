using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

/// <summary>Conversation de démonstration : messages locaux, envoi d'un message fictif, réponse automatique simulée.</summary>
public sealed class ConversationViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IConversationService _conversations;
    private readonly INavigationService _nav;
    private Conversation? _conversation;
    private string _draft = string.Empty;
    private string _error = string.Empty;

    public ConversationViewModel(IConversationService conversations, INavigationService nav)
    {
        _conversations = conversations;
        _nav = nav;
        SendCommand = new AsyncCommand(SendAsync, () => CanSend);
        RestitutionCommand = new AsyncCommand(() => _conversation is null
            ? Task.CompletedTask
            : _nav.GoToAsync(Routes.Restitution, new Dictionary<string, object> { ["reportId"] = _conversation.ReportId }));
    }

    public ICommand SendCommand { get; }
    public ICommand RestitutionCommand { get; }

    /// <summary>Demande à la page de défiler jusqu'au dernier message.</summary>
    public event EventHandler? ScrollRequested;

    public ObservableCollection<ChatMessage> Messages => _conversation?.Messages ?? EmptyMessages;
    private static readonly ObservableCollection<ChatMessage> EmptyMessages = new();

    public string Title => _conversation?.DocumentTitle ?? string.Empty;
    public string WithLine => _conversation is null ? string.Empty : $"Avec {_conversation.CounterpartName} · {_conversation.RoleLabel}";
    public bool IsClosed => _conversation?.IsClosed ?? false;
    public bool IsOpen => !IsClosed;
    public bool HasNoMessages => Messages.Count == 0;

    public string DraftText
    {
        get => _draft;
        set
        {
            if (!SetProperty(ref _draft, value)) return;
            Error = string.Empty;
            OnPropertyChanged(nameof(CanSend));
            (SendCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string Error { get => _error; private set { if (SetProperty(ref _error, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool CanSend => !string.IsNullOrWhiteSpace(DraftText) && !IsClosed;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("conversationId", out var value) && value is Guid id) Load(id);
    }

    private void Load(Guid id)
    {
        if (_conversation is not null) _conversation.Messages.CollectionChanged -= OnMessagesChanged;
        _conversation = _conversations.Get(id);
        if (_conversation is not null) _conversation.Messages.CollectionChanged += OnMessagesChanged;
        RaiseAll();
        ScrollRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasNoMessages));
        ScrollRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>L'écran est ouvert : les messages reçus pendant ce temps ne comptent pas comme non lus.</summary>
    public void OnAppearing()
    {
        if (_conversation is null) return;
        _conversations.SetViewed(_conversation.Id, true);
        OnPropertyChanged(nameof(IsClosed));
        OnPropertyChanged(nameof(IsOpen));
    }

    public void OnDisappearing()
    {
        if (_conversation is not null) _conversations.SetViewed(_conversation.Id, false);
    }

    private async Task SendAsync()
    {
        if (_conversation is null) return;
        if (string.IsNullOrWhiteSpace(DraftText)) { Error = "Écrivez un message avant d’envoyer."; return; }
        var text = DraftText;
        DraftText = string.Empty;
        try
        {
            await _conversations.SendAsync(_conversation.Id, text);
        }
        catch (InvalidOperationException ex)
        {
            Error = ex.Message;
        }
    }
}
