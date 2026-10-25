using System.Windows.Input;

namespace LostFoundDocs.Views.Components;

/// <summary>État vide : icône, titre, message et action facultative.</summary>
public partial class EmptyState : ContentView
{
    public static readonly BindableProperty IconGlyphProperty = Prop(nameof(IconGlyph), string.Empty);
    public static readonly BindableProperty TitleProperty = Prop(nameof(Title), string.Empty);
    public static readonly BindableProperty MessageProperty = Prop(nameof(Message), string.Empty);
    public static readonly BindableProperty ActionTextProperty = Prop(nameof(ActionText), string.Empty);
    public static readonly BindableProperty ActionCommandProperty = Prop<ICommand?>(nameof(ActionCommand), null);

    private static BindableProperty Prop<T>(string name, T defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(EmptyState), defaultValue,
            propertyChanged: (b, _, _) => ((EmptyState)b).OnPropertyChanged(nameof(HasAction)));

    public EmptyState() => InitializeComponent();

    public string IconGlyph { get => (string)GetValue(IconGlyphProperty); set => SetValue(IconGlyphProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string ActionText { get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

    public bool HasAction => !string.IsNullOrEmpty(ActionText) && ActionCommand is not null;
}
