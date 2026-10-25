using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.UI;
using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Messages;

public partial class ConversationPage : ContentPage
{
    private readonly ConversationViewModel _viewModel;

    public ConversationPage(ConversationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.ScrollRequested += OnScrollRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    private async void OnScrollRequested(object? sender, EventArgs e)
    {
        await Task.Delay(120);
        await MainThread.InvokeOnMainThreadAsync(() => Scroller.ScrollToAsync(MessagesStack, ScrollToPosition.End, true));
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) =>
        await ServiceHelper.Get<INavigationService>().GoBackAsync();
}
