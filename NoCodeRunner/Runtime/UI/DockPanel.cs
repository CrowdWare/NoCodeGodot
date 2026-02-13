using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Runtime.UI;

public partial class DockPanel : PanelContainer
{
    [Signal]
    public delegate void DockCommandRequestedEventHandler(string panelId, string command);

    private readonly Dictionary<long, string> _menuCommands = new();
    private Label? _titleLabel;
    private bool _dockMenuConnected;

    public override void _Ready()
    {
        EnsureChrome();
        BuildDockMenu();
    }

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

    public bool IsDockable()
    {
        if (!HasMeta(NodePropertyMapper.MetaDockDockable))
        {
            return true;
        }

        return GetMeta(NodePropertyMapper.MetaDockDockable).AsBool();
    }

    public bool IsDropTarget()
    {
        if (!HasMeta(NodePropertyMapper.MetaDockIsDropTarget))
        {
            return true;
        }

        return GetMeta(NodePropertyMapper.MetaDockIsDropTarget).AsBool();
    }

    private void EnsureChrome()
    {
        if (GetNodeOrNull<Control>("DockPanelRoot/DockHeader") is not null)
        {
            _titleLabel = GetNodeOrNull<Label>("DockPanelRoot/DockHeader/Title");
            if (_titleLabel is not null)
            {
                _titleLabel.Text = ResolvePanelTitle();
                _titleLabel.Visible = false; // TabContainer tab already shows the panel title.
            }
            return;
        }

        var existingChildren = GetChildren().OfType<Node>().ToArray();
        foreach (var child in existingChildren)
        {
            RemoveChild(child);
        }

        var root = new VBoxContainer
        {
            Name = "DockPanelRoot",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        var header = new HBoxContainer
        {
            Name = "DockHeader",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };

        _titleLabel = new Label
        {
            Name = "Title",
            Text = ResolvePanelTitle(),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false
        };

        var menuButton = new MenuButton
        {
            Name = "DockMenuButton",
            Text = "⋮",
            Flat = true,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };

        header.AddChild(_titleLabel);
        header.AddChild(menuButton);

        var content = new VBoxContainer
        {
            Name = "DockContent",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        foreach (var child in existingChildren)
        {
            content.AddChild(child);
        }

        root.AddChild(header);
        root.AddChild(content);

        AddChild(root);
    }

    private void BuildDockMenu()
    {
        var menuButton = GetNodeOrNull<MenuButton>("DockPanelRoot/DockHeader/DockMenuButton");
        if (menuButton is null)
        {
            return;
        }

        if (!IsDockable())
        {
            menuButton.Visible = false;
            return;
        }

        menuButton.Visible = true;

        var popup = menuButton.GetPopup();
        popup.Clear();
        _menuCommands.Clear();

        AddMenuEntry(popup, "Left", "left");
        AddMenuEntry(popup, "Far Left", "far-left");
        AddMenuEntry(popup, "Right", "right");
        AddMenuEntry(popup, "Far Right", "far-right");
        popup.AddSeparator();
        AddMenuEntry(popup, "Bottom Left", "bottom-left");
        AddMenuEntry(popup, "Bottom Far Left", "bottom-far-left");
        AddMenuEntry(popup, "Bottom Right", "bottom-right");
        AddMenuEntry(popup, "Bottom Far Right", "bottom-far-right");
        popup.AddSeparator();
        if (IsFloatable())
        {
            AddMenuEntry(popup, "Floating", "floating");
        }
        if (IsCloseable())
        {
            AddMenuEntry(popup, "Closed", "closed");
        }

        if (!_dockMenuConnected)
        {
            popup.IdPressed += OnDockMenuIdPressed;
            _dockMenuConnected = true;
        }
    }

    private void AddMenuEntry(PopupMenu popup, string title, string command)
    {
        var id = _menuCommands.Count;
        popup.AddItem(title, id);
        _menuCommands[id] = command;
    }

    private void OnDockMenuIdPressed(long id)
    {
        if (!_menuCommands.TryGetValue(id, out var command))
        {
            return;
        }

        EmitSignal(SignalName.DockCommandRequested, ResolvePanelId(), command);
    }

    private bool IsCloseable()
    {
        if (!HasMeta(NodePropertyMapper.MetaDockCloseable))
        {
            return true;
        }

        return GetMeta(NodePropertyMapper.MetaDockCloseable).AsBool();
    }

    private bool IsFloatable()
    {
        if (!HasMeta(NodePropertyMapper.MetaDockFloatable))
        {
            return true;
        }

        return GetMeta(NodePropertyMapper.MetaDockFloatable).AsBool();
    }

}
