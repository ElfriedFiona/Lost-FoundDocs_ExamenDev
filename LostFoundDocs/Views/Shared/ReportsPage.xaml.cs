using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Shared;
public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;
    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.Refresh(); }
}
