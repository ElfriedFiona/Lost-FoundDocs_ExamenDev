using System.Collections.ObjectModel;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class MessagesViewModel : ViewModelBase
{
    private readonly IConversationService _conversations;
    private bool _unreadOnly;
    public MessagesViewModel(IConversationService conversations, INavigationService nav)
    {
        _conversations = conversations;
        OpenCommand = new AsyncCommand<Conversation>(c => c is null ? Task.CompletedTask : nav.GoToAsync(Routes.Conversation, new Dictionary<string, object> { ["reportId"] = c.Report.Id }));
        AllCommand = new Command(() => { _unreadOnly = false; Refresh(); });
        UnreadCommand = new Command(() => { _unreadOnly = true; Refresh(); });
        DeclareCommand = new AsyncCommand(() => nav.GoToAsync(Routes.DeclareLost));
    }
    public ObservableCollection<Conversation> Items { get; } = new();
    public ICommand OpenCommand { get; }
    public ICommand AllCommand { get; }
    public ICommand UnreadCommand { get; }
    public ICommand DeclareCommand { get; }
    public string FilterLabel => _unreadOnly ? "Messages non lus" : "Toutes les conversations";
    public bool IsEmpty => Items.Count == 0;
    public int UnreadCount => _conversations.Conversations.Sum(c => c.Unread);
    public void Refresh()
    {
        Items.Clear();
        foreach (var item in _conversations.Conversations.Where(c => !_unreadOnly || c.Unread > 0)) Items.Add(item);
        OnPropertyChanged(string.Empty);
    }
}
