using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>Encart d'état : information, succès, avertissement, erreur ou neutre.</summary>
public partial class InfoBanner : ContentView
{
    public static readonly BindableProperty KindProperty = Prop(nameof(Kind), BannerKind.Info);
    public static readonly BindableProperty TitleProperty = Prop(nameof(Title), string.Empty);
    public static readonly BindableProperty MessageProperty = Prop(nameof(Message), string.Empty);
    public static readonly BindableProperty IconGlyphProperty = Prop(nameof(IconGlyph), string.Empty);

    private static BindableProperty Prop<T>(string name, T defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(InfoBanner), defaultValue,
            propertyChanged: (b, _, _) => ((InfoBanner)b).Apply());

    public InfoBanner()
    {
        InitializeComponent();
        Apply();
    }

    public BannerKind Kind { get => (BannerKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string IconGlyph { get => (string)GetValue(IconGlyphProperty); set => SetValue(IconGlyphProperty, value); }

    public bool HasTitle => !string.IsNullOrEmpty(Title);
    public bool HasMessage => !string.IsNullOrEmpty(Message);

    private void Apply()
    {
        OnPropertyChanged(nameof(HasTitle));
        OnPropertyChanged(nameof(HasMessage));

        var (soft, stroke, accent, glyph) = Kind switch
        {
            BannerKind.Success => ("SuccessSoft", "SuccessStroke", "Success", Icons.CircleCheck),
            BannerKind.Warning => ("WarningSoft", "WarningStroke", "WarningText", Icons.TriangleAlert),
            BannerKind.Error => ("ErrorSoft", "ErrorStroke", "Error", Icons.CircleAlert),
            BannerKind.Neutral => ("Background", "Border", "Primary", Icons.Lock),
            _ => ("PrimarySoft", "InfoStroke", "Primary", Icons.Info),
        };
        Box.BackgroundColor = Res.GetColor(soft);
        Box.Stroke = new SolidColorBrush(Res.GetColor(stroke));
        IconView.TextColor = Res.GetColor(accent);
        IconView.Text = string.IsNullOrEmpty(IconGlyph) ? glyph : IconGlyph;
        TitleView.TextColor = Res.GetColor("TextPrimary");
    }
}
