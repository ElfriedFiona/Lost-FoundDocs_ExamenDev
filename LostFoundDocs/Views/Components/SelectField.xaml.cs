using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>Liste déroulante stylée (type de document, ville).</summary>
public partial class SelectField : ContentView
{
    private bool _syncing;

    public static readonly BindableProperty LabelTextProperty = Prop(nameof(LabelText), string.Empty);
    public static readonly BindableProperty PlaceholderProperty = Prop(nameof(Placeholder), string.Empty);
    public static readonly BindableProperty HelperTextProperty = Prop(nameof(HelperText), string.Empty);
    public static readonly BindableProperty ErrorTextProperty = Prop(nameof(ErrorText), string.Empty);
    public static readonly BindableProperty IsOptionalProperty = Prop(nameof(IsOptional), false);
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IList<string>), typeof(SelectField), null,
            propertyChanged: (b, _, _) => ((SelectField)b).LoadItems());
    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(string), typeof(SelectField), null, BindingMode.TwoWay,
            propertyChanged: (b, _, _) => ((SelectField)b).SyncSelection());

    private static BindableProperty Prop<T>(string name, T value) =>
        BindableProperty.Create(name, typeof(T), typeof(SelectField), value,
            propertyChanged: (b, _, _) => ((SelectField)b).Refresh());

    public SelectField() => InitializeComponent();

    public string LabelText { get => (string)GetValue(LabelTextProperty); set => SetValue(LabelTextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string HelperText { get => (string)GetValue(HelperTextProperty); set => SetValue(HelperTextProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public bool IsOptional { get => (bool)GetValue(IsOptionalProperty); set => SetValue(IsOptionalProperty, value); }
    public IList<string>? Items { get => (IList<string>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public string? SelectedItem { get => (string?)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    public bool HasLabel => !string.IsNullOrEmpty(LabelText);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);
    public bool ShowHelper => !string.IsNullOrEmpty(HelperText) && !HasError;

    private void Refresh()
    {
        PickerView.Title = Placeholder;
        OnPropertyChanged(nameof(HasLabel));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowHelper));
        FieldBorder.Stroke = Res.GetBrush(HasError ? "ErrorBrush" : "BorderBrush");
        SemanticProperties.SetDescription(PickerView, LabelText);
    }

    private void LoadItems()
    {
        _syncing = true;
        PickerView.ItemsSource = Items?.ToList();
        _syncing = false;
        SyncSelection();
    }

    private void SyncSelection()
    {
        if (_syncing) return;
        _syncing = true;
        PickerView.SelectedItem = SelectedItem;
        _syncing = false;
    }

    private void OnIndexChanged(object? sender, EventArgs e)
    {
        if (_syncing) return;
        _syncing = true;
        SelectedItem = PickerView.SelectedItem as string;
        _syncing = false;
    }
}
