using LostFoundDocs.Models;

namespace LostFoundDocs.Views.Messages;

/// <summary>Choisit l'apparence d'une bulle : message envoyé, message reçu ou message système.</summary>
public sealed class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? MineTemplate { get; set; }
    public DataTemplate? TheirsTemplate { get; set; }
    public DataTemplate? SystemTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ChatMessage message)
        {
            if (message.IsSystem) return SystemTemplate!;
            if (message.IsMine) return MineTemplate!;
        }
        return TheirsTemplate!;
    }
}
