using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Profile;
public partial class SecurityPage : ContentPage
{
    private readonly ProfileSettingsViewModel _viewModel;
    public SecurityPage(ProfileSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.FocusRequested += key =>
        {
            if (key == "CurrentPassword") CurrentPasswordInput.FocusInput();
            if (key == "Password") PasswordInput.FocusInput();
            if (key == "Confirmation") ConfirmationInput.FocusInput();
        };
    }
}
