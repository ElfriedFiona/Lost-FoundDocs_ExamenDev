using System.Globalization;
using LostFoundDocs.Services;
using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>
/// Champ de date : saisie masquée « jj/mm/aaaa » (toujours possible) + calendrier natif via l'icône.
/// Limite connue : choisir dans le calendrier la date déjà affichée par défaut (aujourd'hui) ne déclenche pas
/// d'événement natif ; dans ce cas, saisir la date au clavier.
/// </summary>
public partial class DateField : ContentView
{
    private bool _updating;
    private string _localError = string.Empty;

    public static readonly BindableProperty LabelTextProperty = Prop(nameof(LabelText), string.Empty);
    public static readonly BindableProperty HelperTextProperty = Prop(nameof(HelperText), string.Empty);
    public static readonly BindableProperty ErrorTextProperty = Prop(nameof(ErrorText), string.Empty);
    public static readonly BindableProperty IsOptionalProperty = Prop(nameof(IsOptional), false);
    public static readonly BindableProperty SelectedDateProperty =
        BindableProperty.Create(nameof(SelectedDate), typeof(DateTime?), typeof(DateField), null, BindingMode.TwoWay,
            propertyChanged: (b, _, _) => ((DateField)b).SyncFromValue());

    private static BindableProperty Prop<T>(string name, T value) =>
        BindableProperty.Create(name, typeof(T), typeof(DateField), value,
            propertyChanged: (b, _, _) => ((DateField)b).Refresh());

    public DateField()
    {
        InitializeComponent();
        Picker.MinimumDate = new DateTime(1900, 1, 1);
        Picker.MaximumDate = Clock.Today;
        Picker.Date = Clock.Today;
        Input.TextChanged += OnTextChanged;
    }

    public string LabelText { get => (string)GetValue(LabelTextProperty); set => SetValue(LabelTextProperty, value); }
    public string HelperText { get => (string)GetValue(HelperTextProperty); set => SetValue(HelperTextProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public bool IsOptional { get => (bool)GetValue(IsOptionalProperty); set => SetValue(IsOptionalProperty, value); }
    public DateTime? SelectedDate { get => (DateTime?)GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }

    public string DisplayError => !string.IsNullOrWhiteSpace(ErrorText) ? ErrorText : _localError;
    public bool HasLabel => !string.IsNullOrEmpty(LabelText);
    public bool HasError => !string.IsNullOrWhiteSpace(DisplayError);
    public bool ShowHelper => !string.IsNullOrEmpty(HelperText) && !HasError;

    private void Refresh()
    {
        OnPropertyChanged(nameof(DisplayError));
        OnPropertyChanged(nameof(HasLabel));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowHelper));
        FieldBorder.Stroke = Res.GetBrush(HasError ? "ErrorBrush" : "BorderBrush");
    }

    private void SyncFromValue()
    {
        if (_updating) return;
        var text = SelectedDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        if (Input.Text != text)
        {
            _updating = true;
            Input.Text = text;
            _updating = false;
        }
        if (SelectedDate is { } d && d >= Picker.MinimumDate && d <= Picker.MaximumDate) Picker.Date = d;
        _localError = string.Empty;
        Refresh();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        var digits = new string((e.NewTextValue ?? string.Empty).Where(char.IsDigit).Take(8).ToArray());
        var masked = digits.Length switch
        {
            <= 2 => digits,
            <= 4 => $"{digits[..2]}/{digits[2..]}",
            _ => $"{digits[..2]}/{digits[2..4]}/{digits[4..]}",
        };

        _updating = true;
        if (masked != (e.NewTextValue ?? string.Empty))
        {
            Input.Text = masked;
            Input.CursorPosition = masked.Length;
        }

        DateTime? parsed = null;
        _localError = string.Empty;
        if (masked.Length == 10)
        {
            if (DateTime.TryParseExact(masked, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                parsed = d;
            else
                _localError = "Date invalide (format jj/mm/aaaa).";
        }
        SelectedDate = parsed;
        _updating = false;
        Refresh();
    }

    private void OnPickerDateSelected(object? sender, DateChangedEventArgs e)
    {
        SelectedDate = e.NewDate.Date;
        SyncFromValue();
    }
}
