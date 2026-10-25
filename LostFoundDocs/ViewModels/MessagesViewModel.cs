using System.Collections.ObjectModel;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

public sealed class MessagesViewModel : ViewModelBase
{
    private readonly IConversationService _conversations;
    private readonly INavigationService _nav;

    public MessagesViewModel(IConversationService conversations, INavigationService nav)
    {
        _conversations = conversations;
        _nav = nav;
        OpenCommand = new AsyncCommand<Conversation>(c => c is null
            ? Task.CompletedTask
            : _nav.GoToAsync(Routes.Conversation, new Dictionary<string, object> { ["conversationId"] = c.Id }));
    }

    public ObservableCollection<Conversation> Items { get; } = new();
    public ICommand OpenCommand { get; }
    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => !HasItems;

    public void Refresh()
    {
        Items.Clear();
        foreach (var c in _conversations.Conversations) Items.Add(c);
        RaiseAll();
    }
}
