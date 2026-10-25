namespace LostFoundDocs.UI;

/// <summary>Option d'une liste à choix unique (motifs, lieux de rendez-vous).</summary>
public sealed record OptionItem(string Id, string Title, string? Subtitle = null, string? Glyph = null);
