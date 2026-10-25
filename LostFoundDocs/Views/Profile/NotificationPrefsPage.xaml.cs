using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Profile;

public partial class NotificationPrefsPage : ContentPage
{
    public NotificationPrefsPage(NotificationPrefsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
