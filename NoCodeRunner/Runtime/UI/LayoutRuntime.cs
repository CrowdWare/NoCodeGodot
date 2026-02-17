using Godot;

namespace Runtime.UI;

public static class LayoutRuntime
{
    public static void Apply(Control root)
    {
        // Legacy app/document layout-mode pipeline removed.
        // Layout is now handled directly by Godot anchors, size flags and containers.
    }
}
