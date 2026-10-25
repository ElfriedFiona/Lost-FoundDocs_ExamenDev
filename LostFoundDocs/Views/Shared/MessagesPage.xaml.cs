using LostFoundDocs.Models;
using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Shared;

public partial class MessagesPage : ContentPage
{
    private readonly MessagesViewModel _viewModel;

    public MessagesPage(MessagesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }

    private void OnConversationTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: Conversation conversation })
        {
            _viewModel.OpenCommand.Execute(conversation);
        }
    }
}
