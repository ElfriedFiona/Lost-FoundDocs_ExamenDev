using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Notifications;
public partial class NotificationsPage : ContentPage
{
    private readonly NotificationsViewModel _viewModel;
    public NotificationsPage(NotificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.Refresh(); }
}
