using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Profile;
public partial class HelpPage : ContentPage
{
    private readonly ProfileSettingsViewModel _viewModel;
    public HelpPage(ProfileSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
}
