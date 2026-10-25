using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Matching;

public partial class MatchResultPage : ContentPage
{
    public MatchResultPage(MatchResultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
