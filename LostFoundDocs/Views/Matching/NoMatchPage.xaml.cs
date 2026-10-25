using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Matching;

public partial class NoMatchPage : ContentPage
{
    public NoMatchPage(NoMatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
