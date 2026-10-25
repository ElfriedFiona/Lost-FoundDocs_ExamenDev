using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Messages;

public partial class NotificationsPage : ContentPage
{
    private readonly NotificationsViewModel _viewModel;

    public NotificationsPage(NotificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }

    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: NotificationRow row })
        {
            _viewModel.OpenCommand.Execute(row);
        }
    }
}
