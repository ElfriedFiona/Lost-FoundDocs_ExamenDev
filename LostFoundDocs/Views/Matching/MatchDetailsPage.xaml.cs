using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Matching;
public partial class MatchDetailsPage : ContentPage
{
    private readonly MatchViewModel _viewModel;
    public MatchDetailsPage(MatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.ToggleCriteriaCommand.Execute(null);
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.RefreshCommand.Execute(null); }
}
