using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>Zone de texte multiligne (informations complémentaires).</summary>
public partial class NotesField : ContentView
{
    public static readonly BindableProperty LabelTextProperty = Prop(nameof(LabelText), string.Empty);
    public static readonly BindableProperty PlaceholderProperty = Prop(nameof(Placeholder), string.Empty);
    public static readonly BindableProperty HelperTextProperty = Prop(nameof(HelperText), string.Empty);
    public static readonly BindableProperty ErrorTextProperty = Prop(nameof(ErrorText), string.Empty);
    public static readonly BindableProperty IsOptionalProperty = Prop(nameof(IsOptional), false);
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(NotesField), string.Empty, BindingMode.TwoWay);

    private static BindableProperty Prop<T>(string name, T value) =>
        BindableProperty.Create(name, typeof(T), typeof(NotesField), value,
            propertyChanged: (b, _, _) => ((NotesField)b).Refresh());

    public NotesField() => InitializeComponent();

    public string LabelText { get => (string)GetValue(LabelTextProperty); set => SetValue(LabelTextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string HelperText { get => (string)GetValue(HelperTextProperty); set => SetValue(HelperTextProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public bool IsOptional { get => (bool)GetValue(IsOptionalProperty); set => SetValue(IsOptionalProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);
    public bool ShowHelper => !string.IsNullOrEmpty(HelperText) && !HasError;

    private void Refresh()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowHelper));
        FieldBorder.Stroke = Res.GetBrush(HasError ? "ErrorBrush" : "BorderBrush");
    }
}
