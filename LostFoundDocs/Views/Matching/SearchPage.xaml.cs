using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Matching;

public partial class SearchPage : ContentPage
{
    public SearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
