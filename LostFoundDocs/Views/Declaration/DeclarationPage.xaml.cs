using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Declaration;

public partial class DeclarationPage : ContentPage
{
    private readonly DeclarationViewModel _viewModel;
    public DeclarationPage(DeclarationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.FocusRequested += FocusField;
        foreach (var field in new[] { NameInput, NumberInput, CustomTitleInput, PlaceInput, NotesInput })
            field.ValidationRequested += (_, _) => _viewModel.ValidateCurrent();
    }
    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }
    private async void FocusField(string key)
    {
        VisualElement target = key switch
        {
            "Type" => TypeInput, "CustomTitle" => CustomTitleInput, "Name" => NameInput,
            "Number" => NumberInput, "Birth" => BirthInput, "Date" => DateInput,
            "City" => CityInput, "Place" => PlaceInput, "Notes" => NotesInput,
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
        await FormScroll.ScrollToAsync(target, ScrollToPosition.Center, false);
        if (target is Components.FormField field) field.FocusInput(); else target.Focus();
    }
}
