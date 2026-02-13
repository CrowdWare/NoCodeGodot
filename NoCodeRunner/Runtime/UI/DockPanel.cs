using Godot;

namespace Runtime.UI;

public partial class DockPanel : PanelContainer
{
    public string ResolvePanelId()
    {
        if (HasMeta(NodePropertyMapper.MetaId))
        {
            var id = GetMeta(NodePropertyMapper.MetaId).AsString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return Name;
    }

    public string ResolvePanelTitle()
    {
        if (HasMeta(NodePropertyMapper.MetaDockPanelTitle))
        {
            var title = GetMeta(NodePropertyMapper.MetaDockPanelTitle).AsString();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }

        return ResolvePanelId();
    }

    public DockSlotId ResolveInitialSlot(DockSlotId fallback = DockSlotId.Center)
    {
        if (HasMeta(NodePropertyMapper.MetaDockArea))
        {
            var raw = GetMeta(NodePropertyMapper.MetaDockArea).AsString();
            if (DockSlotIdParser.TryParse(raw, out var slot))
            {
                return slot;
            }
        }

        return fallback;
    }
}
