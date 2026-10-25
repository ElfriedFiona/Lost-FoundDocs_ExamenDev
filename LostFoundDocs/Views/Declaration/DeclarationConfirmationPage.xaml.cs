using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Declaration;

public partial class DeclarationConfirmationPage : ContentPage
{
    public DeclarationConfirmationPage(DeclarationConfirmationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
