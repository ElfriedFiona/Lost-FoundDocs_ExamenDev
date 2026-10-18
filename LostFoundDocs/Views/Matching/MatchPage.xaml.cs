using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Matching;
public partial class MatchPage : ContentPage
{
    private readonly MatchViewModel _viewModel;
    public MatchPage(MatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.RefreshCommand.Execute(null); }
}
