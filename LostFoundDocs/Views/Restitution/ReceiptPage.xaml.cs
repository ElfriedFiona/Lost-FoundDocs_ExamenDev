using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Restitution;

public partial class ReceiptPage : ContentPage
{
    public ReceiptPage(ReceiptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
