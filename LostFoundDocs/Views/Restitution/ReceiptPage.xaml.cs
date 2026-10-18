using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Restitution;
public partial class ReceiptPage : ContentPage
{
    private readonly ReceiptViewModel _viewModel;
    public ReceiptPage(ReceiptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.RefreshCommand.Execute(null); }
}
