using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Profile;
public partial class AboutPage : ContentPage
{
    private readonly ProfileSettingsViewModel _viewModel;
    public AboutPage(ProfileSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
}
