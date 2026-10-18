using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>Champ de formulaire réutilisable : libellé, (facultatif), badge, préfixe, œil, aide et erreur.</summary>
public partial class FormField : ContentView
{
    public event EventHandler? ValidationRequested;
    public void FocusInput() => Input.Focus();
    private bool _hidden = true;
    private bool _focused;

    public static readonly BindableProperty LabelTextProperty = Prop(nameof(LabelText), string.Empty);
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FormField), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = Prop(nameof(Placeholder), string.Empty);
    public static readonly BindableProperty HelperTextProperty = Prop(nameof(HelperText), string.Empty);
    public static readonly BindableProperty ErrorTextProperty = Prop(nameof(ErrorText), string.Empty);
    public static readonly BindableProperty BadgeTextProperty = Prop(nameof(BadgeText), string.Empty);
    public static readonly BindableProperty PrefixTextProperty = Prop(nameof(PrefixText), string.Empty);
    public static readonly BindableProperty IsOptionalProperty = Prop(nameof(IsOptional), false);
    public static readonly BindableProperty IsPasswordProperty = Prop(nameof(IsPassword), false);
    public static readonly BindableProperty KeyboardProperty = Prop(nameof(Keyboard), Keyboard.Default);
    public static readonly BindableProperty MaxLengthProperty = Prop(nameof(MaxLength), int.MaxValue);

    private static BindableProperty Prop<T>(string name, T defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(FormField), defaultValue,
            propertyChanged: (b, _, _) => ((FormField)b).Refresh());

    public FormField()
    {
        InitializeComponent();
        Input.Focused += (_, _) => { _focused = true; UpdateStroke(); };
        Input.Unfocused += (_, _) => { _focused = false; UpdateStroke(); ValidationRequested?.Invoke(this, EventArgs.Empty); };
    }

    public string LabelText { get => (string)GetValue(LabelTextProperty); set => SetValue(LabelTextProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string HelperText { get => (string)GetValue(HelperTextProperty); set => SetValue(HelperTextProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public string BadgeText { get => (string)GetValue(BadgeTextProperty); set => SetValue(BadgeTextProperty, value); }
    public string PrefixText { get => (string)GetValue(PrefixTextProperty); set => SetValue(PrefixTextProperty, value); }
    public bool IsOptional { get => (bool)GetValue(IsOptionalProperty); set => SetValue(IsOptionalProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

    public bool HasLabel => !string.IsNullOrEmpty(LabelText);
    public bool HasBadge => !string.IsNullOrEmpty(BadgeText);
    public bool HasPrefix => !string.IsNullOrEmpty(PrefixText);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);
    public bool ShowHelper => !string.IsNullOrEmpty(HelperText) && !HasError;
    public bool IsTextHidden => IsPassword && _hidden;
    public string EyeGlyph => _hidden ? Icons.Eye : Icons.EyeOff;

    private void Refresh()
    {
        OnPropertyChanged(nameof(HasLabel));
        OnPropertyChanged(nameof(HasBadge));
        OnPropertyChanged(nameof(HasPrefix));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowHelper));
        OnPropertyChanged(nameof(IsTextHidden));
        OnPropertyChanged(nameof(EyeGlyph));
        UpdateStroke();
    }

    private void UpdateStroke()
    {
        FieldBorder.Stroke = HasError
            ? Res.GetBrush("ErrorBrush")
            : _focused ? Res.GetBrush("PrimaryBrush") : Res.GetBrush("BorderBrush");
    }

    private void OnEyeTapped(object? sender, TappedEventArgs e)
    {
        _hidden = !_hidden;
        OnPropertyChanged(nameof(IsTextHidden));
        OnPropertyChanged(nameof(EyeGlyph));
    }
}
