using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Profile;

public partial class AboutPage : ContentPage
{
    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
