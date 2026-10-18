using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Profile;
public partial class PrivacyPage : ContentPage
{
    private readonly ProfileSettingsViewModel _viewModel;
    public PrivacyPage(ProfileSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
}
