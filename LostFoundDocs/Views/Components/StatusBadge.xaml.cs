using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>Pastille de statut (point ou coche + libellé). Le texte n'est jamais seul porteur de la couleur.</summary>
public partial class StatusBadge : ContentView
{
    public static readonly BindableProperty TextProperty = Prop(nameof(Text), string.Empty);
    public static readonly BindableProperty KindProperty = Prop(nameof(Kind), BadgeKind.Neutral);
    public static readonly BindableProperty ShowCheckProperty = Prop(nameof(ShowCheck), false);

    private static BindableProperty Prop<T>(string name, T defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(StatusBadge), defaultValue,
            propertyChanged: (b, _, _) => ((StatusBadge)b).Apply());

    public StatusBadge()
    {
        InitializeComponent();
        Apply();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public BadgeKind Kind { get => (BadgeKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public bool ShowCheck { get => (bool)GetValue(ShowCheckProperty); set => SetValue(ShowCheckProperty, value); }

    private void Apply()
    {
        var (bg, fg, dot) = Kind switch
        {
            BadgeKind.Success => ("SuccessSoft", "SuccessText", "Success"),
            BadgeKind.Warning => ("WarningSoft", "WarningText", "Warning"),
            BadgeKind.Info => ("PrimarySoft", "Primary", "Primary"),
            BadgeKind.Error => ("ErrorSoft", "ErrorText", "Error"),
            _ => ("NeutralSoft", "TextSecondary", "Placeholder"),
        };
        Pill.BackgroundColor = Res.GetColor(bg);
        TextView.TextColor = Res.GetColor(fg);
        TextView.Text = Text;
        Dot.Color = Res.GetColor(dot);
        Dot.IsVisible = !ShowCheck;
        CheckView.IsVisible = ShowCheck;
        CheckView.TextColor = Res.GetColor(fg);
        SemanticProperties.SetDescription(this, $"Statut : {Text}");
    }
}
