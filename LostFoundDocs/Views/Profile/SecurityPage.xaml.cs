using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Profile;

public partial class SecurityPage : ContentPage
{
    public SecurityPage(SecurityViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
