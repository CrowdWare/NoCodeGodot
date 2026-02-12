using Runtime.Sml;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed class TreeViewItem
{
    public int Id { get; init; }
    public required string Text { get; init; }
    public string? Icon { get; init; }
    public bool Expanded { get; init; }
    public SmlNode? Data { get; init; }
    public List<TreeViewItem> Children { get; } = [];
    public List<TreeViewToggle> Toggles { get; } = [];
}

public sealed class TreeViewToggle
{
    public required ToggleId ToggleId { get; init; }
    public required string Name { get; init; }
    public bool State { get; set; }
    public required string ImageOn { get; init; }
    public required string ImageOff { get; init; }
}
