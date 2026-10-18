using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Profile;
public partial class PersonalPage : ContentPage
{
    private readonly ProfileSettingsViewModel _viewModel;
    public PersonalPage(ProfileSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.FocusRequested += key =>
        {
            if (key == "Name") NameInput.FocusInput();
            if (key == "Email") EmailInput.FocusInput();
        };
    }
}
