using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Profile;
public partial class PreferencesPage : ContentPage
{
    private readonly ProfileSettingsViewModel _viewModel;
    public PreferencesPage(ProfileSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
}
