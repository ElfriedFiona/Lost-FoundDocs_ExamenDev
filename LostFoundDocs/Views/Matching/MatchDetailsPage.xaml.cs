using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Matching;

public partial class MatchDetailsPage : ContentPage
{
    public MatchDetailsPage(MatchDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
