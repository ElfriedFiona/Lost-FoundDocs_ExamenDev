using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Profile;

public partial class PersonalInfoPage : ContentPage
{
    public PersonalInfoPage(PersonalInfoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
