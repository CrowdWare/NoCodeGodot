using Godot;
using System;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed partial class DockingContainerControl : PanelContainer
{
    private static readonly List<DockingContainerControl> ActiveInstances = [];

    public enum DockSideKind
    {
        Center,
        FarLeft,
        FarLeftBottom,
        Left,
        LeftBottom,
        Right,
        RightBottom,
        FarRight,
        FarRightBottom
    }

    private const float DefaultMinFixedWidth = 140f;
    private const float DefaultMinFixedHeight = 80f;
    private VBoxContainer? _root;
    private HBoxContainer? _header;
    private TabBar? _tabBar;
    private MenuButton? _menuButton;
    private Control? _contentContainer;
    private readonly List<DockTabEntry> _tabs = [];

    private sealed class DockTabEntry
    {
        public required Control Content;
        public required string Title;
    }

    public override void _EnterTree()
    {
        if (!ActiveInstances.Contains(this))
        {
            ActiveInstances.Add(this);
        }
    }

    public override void _ExitTree()
    {
        ActiveInstances.Remove(this);
    }

    public override void _Ready()
    {
        EnsureDockStructure();
        EnsureDockDefaults();
        SyncTabVisibility();
    }

    public TabBar EnsureTabBar()
    {
        EnsureDockStructure();
        return _tabBar!;
    }

    public MenuButton EnsureMenuButton()
    {
        EnsureDockStructure();
        return _menuButton!;
    }

    public void EnsureDockStructure()
    {
        if (_root is not null
            && GodotObject.IsInstanceValid(_root)
            && _tabBar is not null
            && GodotObject.IsInstanceValid(_tabBar)
            && _contentContainer is not null
            && GodotObject.IsInstanceValid(_contentContainer)
            && _menuButton is not null
            && GodotObject.IsInstanceValid(_menuButton))
        {
            return;
        }

        var legacyEntries = new List<DockTabEntry>();
        var looseControls = new List<Control>();
        var children = new List<Node>();

        for (var i = 0; i < GetChildCount(); i++)
        {
            children.Add(GetChild(i));
        }

        foreach (var child in children)
        {
            if (child is TabContainer legacyTabs)
            {
                for (var tabIndex = 0; tabIndex < legacyTabs.GetTabCount(); tabIndex++)
                {
                    var tabControl = legacyTabs.GetTabControl(tabIndex);
                    if (tabControl is null)
                    {
                        continue;
                    }

                    var title = legacyTabs.GetTabTitle(tabIndex);
                    legacyTabs.RemoveChild(tabControl);
                    legacyEntries.Add(new DockTabEntry
                    {
                        Content = tabControl,
                        Title = string.IsNullOrWhiteSpace(title) ? tabControl.Name : title
                    });
                }

                RemoveChild(legacyTabs);
                legacyTabs.QueueFree();
                continue;
            }

            if (child is Control control)
            {
                RemoveChild(control);
                looseControls.Add(control);
            }
        }

        _root = new VBoxContainer
        {
            Name = string.IsNullOrWhiteSpace(Name) ? "DockRoot" : $"{Name}_DockRoot",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        _header = new HBoxContainer
        {
            Name = "Header",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };

        _tabBar = new TabBar
        {
            Name = "Tabs",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            ClipTabs = true,
            DragToRearrangeEnabled = true,
            TabsRearrangeGroup = 0
        };
        _tabBar.TabChanged += OnTabChanged;
        _tabBar.ActiveTabRearranged += OnActiveTabRearranged;

        _menuButton = new MenuButton
        {
            Name = "DockMenu",
            Text = "⋮",
            TooltipText = "Dock menu",
            Flat = true,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(24, 24)
        };

        var baseMenuFont = _menuButton.GetThemeFont("font");
        if (baseMenuFont is not null)
        {
            var boldMenuFont = new FontVariation
            {
                BaseFont = baseMenuFont,
                VariationEmbolden = 0.8f
            };
            _menuButton.AddThemeFontOverride("font", boldMenuFont);
        }

        _contentContainer = new Control
        {
            Name = "Content",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _contentContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _header.AddChild(_tabBar);
        _header.AddChild(_menuButton);
        _root.AddChild(_header);
        _root.AddChild(_contentContainer);

        AddChild(_root);
        MoveChild(_root, 0);

        foreach (var entry in legacyEntries)
        {
            AddDockTab(entry.Content, entry.Title);
        }

        foreach (var control in looseControls)
        {
            AddDockTab(control, ResolveTabTitle(control));
        }
    }

    public void AddDockTab(Control child, string title)
    {
        EnsureDockStructure();
        var safeTitle = string.IsNullOrWhiteSpace(title) ? ResolveTabTitle(child) : title;

        if (child.GetParent() == _contentContainer)
        {
            var existingIndex = GetTabIndexFromControl(child);
            if (existingIndex >= 0)
            {
                SetTabTitle(existingIndex, safeTitle);
                SyncTabVisibility();
                return;
            }

            _tabs.Add(new DockTabEntry
            {
                Content = child,
                Title = safeTitle
            });
            _tabBar!.AddTab(safeTitle);
            SyncTabVisibility();
            return;
        }

        if (child.GetParent() is Node oldParent)
        {
            oldParent.RemoveChild(child);
        }

        _contentContainer!.AddChild(child);
        child.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        child.SizeFlagsVertical = SizeFlags.ExpandFill;
        child.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _tabs.Add(new DockTabEntry
        {
            Content = child,
            Title = safeTitle
        });

        if (FindTabBarIndexByTitle(safeTitle) < 0)
        {
            _tabBar!.AddTab(safeTitle);
        }

        if (_tabBar!.GetTabCount() == 1)
        {
            _tabBar.CurrentTab = 0;
        }

        SyncTabVisibility();
    }

    public int GetTabCount()
    {
        EnsureDockStructure();
        return _tabBar!.GetTabCount();
    }

    public int GetCurrentTab()
    {
        EnsureDockStructure();
        return _tabBar!.CurrentTab;
    }

    public void SetCurrentTab(int index)
    {
        EnsureDockStructure();
        var tabCount = _tabBar!.GetTabCount();
        if (tabCount <= 0)
        {
            return;
        }

        var clamped = Math.Clamp(index, 0, tabCount - 1);
        _tabBar!.CurrentTab = clamped;
        SyncTabVisibility();
    }

    public Control? GetTabControl(int index)
    {
        EnsureDockStructure();
        if (index < 0 || index >= _tabBar!.GetTabCount())
        {
            return null;
        }

        var title = _tabBar.GetTabTitle(index);
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (string.Equals(_tabs[i].Title, title, StringComparison.Ordinal)
                && _tabs[i].Content.GetParent() == _contentContainer)
            {
                return _tabs[i].Content;
            }
        }

        return index < _tabs.Count ? _tabs[index].Content : null;
    }

    public string GetTabTitle(int index)
    {
        EnsureDockStructure();
        if (index < 0 || index >= _tabBar!.GetTabCount())
        {
            return string.Empty;
        }

        return _tabBar.GetTabTitle(index);
    }

    public void SetTabTitle(int index, string title)
    {
        EnsureDockStructure();
        if (index < 0 || index >= _tabBar!.GetTabCount())
        {
            return;
        }

        var oldTitle = _tabBar.GetTabTitle(index);
        var safeTitle = string.IsNullOrWhiteSpace(title) ? ResolveTabTitle(_tabs[index].Content) : title;
        _tabBar.SetTabTitle(index, safeTitle);

        var matched = false;
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (!string.Equals(_tabs[i].Title, oldTitle, StringComparison.Ordinal))
            {
                continue;
            }

            _tabs[i].Title = safeTitle;
            matched = true;
            break;
        }

        if (!matched && index < _tabs.Count)
        {
            _tabs[index].Title = safeTitle;
        }
    }

    public int GetTabIndexFromControl(Control control)
    {
        EnsureDockStructure();
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (ReferenceEquals(_tabs[i].Content, control))
            {
                return i;
            }
        }

        return -1;
    }

    public void RemoveDockTab(Control control)
    {
        EnsureDockStructure();
        var index = GetTabIndexFromControl(control);
        if (index < 0)
        {
            if (control.GetParent() == _contentContainer)
            {
                _contentContainer!.RemoveChild(control);
            }

            return;
        }

        var wasCurrent = _tabBar!.CurrentTab == index;
        _tabs.RemoveAt(index);
        _tabBar.RemoveTab(index);
        _contentContainer!.RemoveChild(control);

        if (_tabs.Count == 0)
        {
            SyncTabVisibility();
            return;
        }

        if (wasCurrent)
        {
            _tabBar.CurrentTab = Math.Min(index, _tabs.Count - 1);
        }

        SyncTabVisibility();
    }

    public bool IsDragToRearrangeEnabled()
    {
        EnsureDockStructure();

        if (HasMeta(NodePropertyMapper.MetaDockDragToRearrangeEnabled))
        {
            return GetMeta(NodePropertyMapper.MetaDockDragToRearrangeEnabled).AsBool();
        }

        return _tabBar!.DragToRearrangeEnabled;
    }

    public int GetTabsRearrangeGroup()
    {
        EnsureDockStructure();

        if (HasMeta(NodePropertyMapper.MetaDockTabsRearrangeGroup))
        {
            return GetMeta(NodePropertyMapper.MetaDockTabsRearrangeGroup).AsInt32();
        }

        return _tabBar!.TabsRearrangeGroup;
    }

    public DockSideKind GetDockSideKind()
    {
        if (!HasMeta(NodePropertyMapper.MetaDockSide))
        {
            return DockSideKind.Center;
        }

        var side = (GetMeta(NodePropertyMapper.MetaDockSide).AsString() ?? string.Empty).Trim().ToLowerInvariant();
        return side switch
        {
            "farleft" => DockSideKind.FarLeft,
            "farleftbottom" => DockSideKind.FarLeftBottom,
            "left" => DockSideKind.Left,
            "leftbottom" => DockSideKind.LeftBottom,
            "right" => DockSideKind.Right,
            "rightbottom" => DockSideKind.RightBottom,
            "farright" => DockSideKind.FarRight,
            "farrightbottom" => DockSideKind.FarRightBottom,
            _ => DockSideKind.Center
        };
    }

    public string GetDockSide()
    {
        return GetDockSideKind() switch
        {
            DockSideKind.FarLeft => "farleft",
            DockSideKind.FarLeftBottom => "farleftbottom",
            DockSideKind.Left => "left",
            DockSideKind.LeftBottom => "leftbottom",
            DockSideKind.Right => "right",
            DockSideKind.RightBottom => "rightbottom",
            DockSideKind.FarRight => "farright",
            DockSideKind.FarRightBottom => "farrightbottom",
            _ => "center"
        };
    }

    public bool IsFlex()
    {
        if (HasMeta(NodePropertyMapper.MetaDockFlex))
        {
            return GetMeta(NodePropertyMapper.MetaDockFlex).AsBool();
        }

        return GetDockSideKind() == DockSideKind.Center;
    }

    public float GetFixedWidth()
    {
        var minFixedWidth = GetMinFixedWidth();

        if (HasMeta(NodePropertyMapper.MetaDockFixedWidth))
        {
            var width = Math.Max(0, GetMeta(NodePropertyMapper.MetaDockFixedWidth).AsInt32());
            return Math.Max(minFixedWidth, width);
        }

        var minWidth = CustomMinimumSize.X;
        var fallback = minWidth > 0 ? minWidth : 240f;
        return Math.Max(minFixedWidth, fallback);
    }

    public float GetMinFixedWidth()
    {
        if (HasMeta(NodePropertyMapper.MetaDockMinFixedWidth))
        {
            return Math.Max(40f, GetMeta(NodePropertyMapper.MetaDockMinFixedWidth).AsInt32());
        }

        return DefaultMinFixedWidth;
    }

    public bool HasFixedHeight()
    {
        return HasMeta(NodePropertyMapper.MetaDockFixedHeight);
    }

    public float GetFixedHeight()
    {
        var minFixedHeight = GetMinFixedHeight();

        if (HasMeta(NodePropertyMapper.MetaDockFixedHeight))
        {
            var height = Math.Max(0, GetMeta(NodePropertyMapper.MetaDockFixedHeight).AsInt32());
            return Math.Max(minFixedHeight, height);
        }

        var minHeight = CustomMinimumSize.Y;
        var fallback = minHeight > 0 ? minHeight : 160f;
        return Math.Max(minFixedHeight, fallback);
    }

    public float GetMinFixedHeight()
    {
        if (HasMeta(NodePropertyMapper.MetaDockMinFixedHeight))
        {
            return Math.Max(24f, GetMeta(NodePropertyMapper.MetaDockMinFixedHeight).AsInt32());
        }

        return DefaultMinFixedHeight;
    }

    public bool HasHeightPercent()
    {
        return HasMeta(NodePropertyMapper.MetaDockHeightPercent);
    }

    public float GetHeightPercent()
    {
        if (!HasMeta(NodePropertyMapper.MetaDockHeightPercent))
        {
            return 50f;
        }

        return Mathf.Clamp((float)GetMeta(NodePropertyMapper.MetaDockHeightPercent).AsDouble(), 0f, 100f);
    }

    public void SetFixedWidth(float width)
    {
        var clamped = Math.Max(GetMinFixedWidth(), width);
        SetMeta(NodePropertyMapper.MetaDockFixedWidth, Variant.From((int)MathF.Round(clamped)));
    }

    public bool IsCloseable()
    {
        if (HasMeta(NodePropertyMapper.MetaDockCloseable))
        {
            return GetMeta(NodePropertyMapper.MetaDockCloseable).AsBool();
        }

        return true;
    }

    private void EnsureDockDefaults()
    {
        EnsureDockStructure();

        if (HasMeta(NodePropertyMapper.MetaDockDragToRearrangeEnabled))
        {
            _tabBar!.DragToRearrangeEnabled = GetMeta(NodePropertyMapper.MetaDockDragToRearrangeEnabled).AsBool();
        }

        if (HasMeta(NodePropertyMapper.MetaDockTabsRearrangeGroup))
        {
            _tabBar!.TabsRearrangeGroup = GetMeta(NodePropertyMapper.MetaDockTabsRearrangeGroup).AsInt32();
        }
    }

    private void OnTabChanged(long _index)
    {
        TryAdoptExternalTabsByTitle();
        SyncTabVisibility();
    }

    private void OnActiveTabRearranged(long _index)
    {
        TryAdoptExternalTabsByTitle();

        if (_tabBar is null)
        {
            return;
        }

        var tabCount = _tabBar.GetTabCount();
        if (tabCount != _tabs.Count)
        {
            return;
        }

        var reordered = new List<DockTabEntry>(_tabs.Count);
        var used = new bool[_tabs.Count];

        for (var i = 0; i < tabCount; i++)
        {
            var title = _tabBar.GetTabTitle(i);
            var matchedIndex = -1;

            for (var j = 0; j < _tabs.Count; j++)
            {
                if (used[j])
                {
                    continue;
                }

                if (!string.Equals(_tabs[j].Title, title, StringComparison.Ordinal))
                {
                    continue;
                }

                matchedIndex = j;
                break;
            }

            if (matchedIndex < 0)
            {
                for (var j = 0; j < _tabs.Count; j++)
                {
                    if (!used[j])
                    {
                        matchedIndex = j;
                        break;
                    }
                }
            }

            if (matchedIndex < 0)
            {
                continue;
            }

            used[matchedIndex] = true;
            reordered.Add(_tabs[matchedIndex]);
        }

        if (reordered.Count == _tabs.Count)
        {
            _tabs.Clear();
            _tabs.AddRange(reordered);
        }

        SyncTabVisibility();
    }

    private void TryAdoptExternalTabsByTitle()
    {
        if (_tabBar is null)
        {
            return;
        }

        var tabCount = _tabBar.GetTabCount();
        for (var i = 0; i < tabCount; i++)
        {
            var title = _tabBar.GetTabTitle(i);
            if (HasLocalTabEntry(title))
            {
                continue;
            }

            if (!TryExtractFromOtherContainer(title, out var content) || content is null)
            {
                continue;
            }

            AttachAdoptedContent(content, title);
        }
    }

    private bool TryExtractFromOtherContainer(string title, out Control? content)
    {
        foreach (var instance in ActiveInstances)
        {
            if (ReferenceEquals(instance, this) || !GodotObject.IsInstanceValid(instance))
            {
                continue;
            }

            content = instance.ExtractDockTabByTitle(title);
            if (content is not null)
            {
                return true;
            }
        }

        content = null;
        return false;
    }

    private Control? ExtractDockTabByTitle(string title)
    {
        EnsureDockStructure();

        var entryIndex = FindEntryIndexByTitle(title);
        if (entryIndex < 0)
        {
            return null;
        }

        var content = _tabs[entryIndex].Content;
        var tabIndex = FindTabBarIndexByTitle(title);
        var wasCurrent = tabIndex >= 0 && _tabBar!.CurrentTab == tabIndex;

        _tabs.RemoveAt(entryIndex);
        if (tabIndex >= 0)
        {
            _tabBar!.RemoveTab(tabIndex);
        }

        if (content.GetParent() == _contentContainer)
        {
            _contentContainer!.RemoveChild(content);
        }

        if (wasCurrent && _tabBar!.GetTabCount() > 0)
        {
            _tabBar.CurrentTab = Math.Clamp(tabIndex, 0, _tabBar.GetTabCount() - 1);
        }

        SyncTabVisibility();
        return content;
    }

    private void AttachAdoptedContent(Control content, string title)
    {
        if (content.GetParent() is Node oldParent)
        {
            oldParent.RemoveChild(content);
        }

        _contentContainer!.AddChild(content);
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _tabs.Add(new DockTabEntry
        {
            Content = content,
            Title = title
        });
    }

    private bool HasLocalTabEntry(string title)
    {
        return FindEntryIndexByTitle(title) >= 0;
    }

    private int FindEntryIndexByTitle(string title)
    {
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (!string.Equals(_tabs[i].Title, title, StringComparison.Ordinal))
            {
                continue;
            }

            if (_tabs[i].Content.GetParent() == _contentContainer)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindTabBarIndexByTitle(string title)
    {
        if (_tabBar is null)
        {
            return -1;
        }

        for (var i = 0; i < _tabBar.GetTabCount(); i++)
        {
            if (string.Equals(_tabBar.GetTabTitle(i), title, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void SyncTabVisibility()
    {
        if (_tabBar is null || _contentContainer is null)
        {
            return;
        }

        var tabBarCount = _tabBar.GetTabCount();
        if (_tabs.Count == 0 || tabBarCount == 0)
        {
            foreach (var tab in _tabs)
            {
                tab.Content.Visible = false;
            }

            return;
        }

        var maxValidIndex = Math.Min(_tabs.Count, tabBarCount) - 1;
        if (maxValidIndex < 0)
        {
            return;
        }

        var currentTab = _tabBar.CurrentTab;
        if (currentTab < 0 || currentTab > maxValidIndex)
        {
            currentTab = Math.Clamp(currentTab, 0, maxValidIndex);
            _tabBar.CurrentTab = currentTab;
        }

        var selectedTitle = _tabBar.GetTabTitle(currentTab);
        var selectedFound = false;

        for (var i = 0; i < _tabs.Count; i++)
        {
            var matchesTitle = string.Equals(_tabs[i].Title, selectedTitle, StringComparison.Ordinal);
            _tabs[i].Content.Visible = matchesTitle && !selectedFound;
            if (_tabs[i].Content.Visible)
            {
                selectedFound = true;
            }
        }

        if (selectedFound)
        {
            return;
        }

        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Content.Visible = i <= maxValidIndex && i == currentTab;
        }
    }

    private static string ResolveTabTitle(Control child)
    {
        return child.HasMeta("sml_ctx_tabTitle")
            ? child.GetMeta("sml_ctx_tabTitle").AsString()
            : string.IsNullOrWhiteSpace(child.Name)
                ? "Panel"
                : child.Name;
    }
}
