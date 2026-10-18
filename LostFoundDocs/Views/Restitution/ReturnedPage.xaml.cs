using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Restitution;
public partial class ReturnedPage : ContentPage
{
    private readonly ReceiptViewModel _viewModel;
    public ReturnedPage(ReceiptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.RefreshCommand.Execute(null); }
}
