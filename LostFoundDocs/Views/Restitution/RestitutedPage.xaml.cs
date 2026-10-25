using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Restitution;

public partial class RestitutedPage : ContentPage
{
    public RestitutedPage(RestitutedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
