using System;

namespace Runtime.UI;

public enum DockSlotId
{
    Left,
    FarLeft,
    Right,
    FarRight,
    BottomLeft,
    BottomFarLeft,
    BottomRight,
    BottomFarRight,
    Center
}

public static class DockSlotIdParser
{
    public static bool TryParse(string? raw, out DockSlotId slot)
    {
        slot = DockSlotId.Center;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim().ToLowerInvariant().Replace("_", "-", StringComparison.Ordinal);
        switch (normalized)
        {
            case "left":
                slot = DockSlotId.Left;
                return true;
            case "far-left":
                slot = DockSlotId.FarLeft;
                return true;
            case "right":
                slot = DockSlotId.Right;
                return true;
            case "far-right":
                slot = DockSlotId.FarRight;
                return true;
            case "bottom-left":
                slot = DockSlotId.BottomLeft;
                return true;
            case "bottom-far-left":
                slot = DockSlotId.BottomFarLeft;
                return true;
            case "bottom-right":
                slot = DockSlotId.BottomRight;
                return true;
            case "bottom-far-right":
                slot = DockSlotId.BottomFarRight;
                return true;
            case "center":
                slot = DockSlotId.Center;
                return true;
            default:
                return false;
        }
    }

    public static string ToSmlValue(this DockSlotId slot)
    {
        return slot switch
        {
            DockSlotId.Left => "left",
            DockSlotId.FarLeft => "far-left",
            DockSlotId.Right => "right",
            DockSlotId.FarRight => "far-right",
            DockSlotId.BottomLeft => "bottom-left",
            DockSlotId.BottomFarLeft => "bottom-far-left",
            DockSlotId.BottomRight => "bottom-right",
            DockSlotId.BottomFarRight => "bottom-far-right",
            _ => "center"
        };
    }
}
