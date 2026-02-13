using System;

namespace Runtime.UI;

public sealed class DockPanelState
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DockSlotId CurrentSlot { get; set; } = DockSlotId.Center;
    public DockSlotId LastDockedSlot { get; set; } = DockSlotId.Center;
    public int TabIndex { get; set; }
    public bool IsActiveTab { get; set; }
    public bool IsFloating { get; set; }
    public bool IsClosed { get; set; }
}

public sealed class DockLayoutState
{
    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;
    public DockPanelState[] Panels { get; init; } = Array.Empty<DockPanelState>();
}
