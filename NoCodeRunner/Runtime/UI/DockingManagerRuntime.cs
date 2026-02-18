using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;

namespace Runtime.UI;

public static partial class DockingManagerRuntime
{
    public static void AttachIfNeeded(Control root)
    {
        var hosts = CollectDockHosts(root);
        if (hosts.Count < 2)
        {
            return;
        }

        var manager = new DockingManagerNode();
        manager.Initialize(hosts);
        root.AddChild(manager);
        RunnerLogger.Info("UI", $"DockingManager attached for {hosts.Count} dock host(s).");
    }

    private static List<DockHostState> CollectDockHosts(Control root)
    {
        var result = new List<DockHostState>();
        var scopedHost = FindScopedDockingHost(root);
        var scopedMode = scopedHost is not null;
        var stack = new Stack<Control>();
        stack.Push(scopedHost ?? root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is TabContainer tabs && tabs.DragToRearrangeEnabled)
            {
                if (tabs.GetParent() is Control hostContainer)
                {
                    if (scopedMode && hostContainer is not DockingContainerControl)
                    {
                        continue;
                    }

                    var hostName = ResolveHostName(tabs, hostContainer, result.Count + 1);
                    result.Add(new DockHostState(hostName, hostContainer, tabs));
                }
            }

            for (var i = current.GetChildCount() - 1; i >= 0; i--)
            {
                if (current.GetChild(i) is Control childControl)
                {
                    stack.Push(childControl);
                }
            }
        }

        return result;
    }

    private static Control? FindScopedDockingHost(Control root)
    {
        var stack = new Stack<Control>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is DockingHostControl)
            {
                return current;
            }

            for (var i = current.GetChildCount() - 1; i >= 0; i--)
            {
                if (current.GetChild(i) is Control childControl)
                {
                    stack.Push(childControl);
                }
            }
        }

        return null;
    }

    private static string ResolveHostName(TabContainer tabs, Control hostContainer, int fallbackIndex)
    {
        if (tabs.HasMeta(NodePropertyMapper.MetaId))
        {
            var id = tabs.GetMeta(NodePropertyMapper.MetaId).AsString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        if (!string.IsNullOrWhiteSpace(tabs.Name))
        {
            return tabs.Name;
        }

        if (!string.IsNullOrWhiteSpace(hostContainer.Name))
        {
            return hostContainer.Name;
        }

        return $"dockHost{fallbackIndex}";
    }

    private sealed partial class DockingManagerNode : Node
    {
        private readonly List<DockHostState> _hosts = [];
        private readonly List<FlexibleHostState> _flexibleHosts = [];
        private readonly Dictionary<long, MenuAction> _menuActions = [];
        private double _elapsed;
        private float _baseLeftInset;
        private float _baseRightInset;

        public void Initialize(List<DockHostState> hosts)
        {
            _hosts.AddRange(hosts);

            foreach (var host in _hosts)
            {
                host.Side = DetermineSide(host.HostContainer);
                host.BaseWidth = EstimateHostWidth(host.HostContainer);
            }

            _baseLeftInset = ComputeCurrentInset(DockSide.Left);
            _baseRightInset = ComputeCurrentInset(DockSide.Right);
            CollectFlexibleHosts();

            foreach (var host in _hosts)
            {
                host.LastKnownTabCount = host.Tabs.GetTabCount();
                CreateKebabMenu(host);
            }

            UpdateFlexibleHostsLayout();
            RefreshAllMenus();
        }

        public override void _Process(double delta)
        {
            _elapsed += delta;
            if (_elapsed < 0.2)
            {
                return;
            }

            _elapsed = 0;
            var changed = false;

            foreach (var host in _hosts)
            {
                if (!GodotObject.IsInstanceValid(host.HostContainer)
                    || !GodotObject.IsInstanceValid(host.Tabs))
                {
                    continue;
                }

                if (host.HostContainer.Visible && host.Tabs.GetTabCount() == 0)
                {
                    // Empty hosts should be auto-hidden regardless of closeable flag.
                    host.HostContainer.Visible = false;
                    host.IsClosed = true;
                    host.LastKnownTabCount = 0;
                    changed = true;
                }
                else
                {
                    host.LastKnownTabCount = host.Tabs.GetTabCount();
                }
            }

            if (changed)
            {
                UpdateFlexibleHostsLayout();
                RefreshAllMenus();
            }
        }

        private void CreateKebabMenu(DockHostState host)
        {
            if (!host.CanClose)
            {
                return;
            }

            var button = new MenuButton
            {
                Name = $"DockKebab_{host.Name}",
                Text = "⋮",
                TooltipText = "Dock menu",
                Flat = true,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
                CustomMinimumSize = new Vector2(28, 24),
                MouseFilter = Control.MouseFilterEnum.Stop,
                ZIndex = 1000
            };

            button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
            button.SetOffset(Side.Right, -4);
            button.SetOffset(Side.Top, 2);
            button.SetOffset(Side.Left, -34);
            button.SetOffset(Side.Bottom, 26);

            host.HostContainer.AddChild(button);
            host.KebabButton = button;

            var popup = button.GetPopup();
            popup.IdPressed += id =>
            {
                if (!_menuActions.TryGetValue(id, out var action))
                {
                    return;
                }

                ExecuteMoveAction(action.SourceHost, action.TargetHost);
            };
        }

        private void ExecuteMoveAction(DockHostState sourceHost, DockHostState targetHost)
        {
            if (!GodotObject.IsInstanceValid(sourceHost.Tabs) || !GodotObject.IsInstanceValid(targetHost.Tabs))
            {
                return;
            }

            var sourceTabs = sourceHost.Tabs;
            var targetTabs = targetHost.Tabs;

            if (sourceTabs.GetTabCount() <= 0)
            {
                targetHost.HostContainer.Visible = true;
                targetHost.IsClosed = false;
                RefreshAllMenus();
                return;
            }

            var sourceIndex = sourceTabs.CurrentTab;
            if (sourceIndex < 0 || sourceIndex >= sourceTabs.GetTabCount())
            {
                sourceIndex = 0;
            }

            var movedControl = sourceTabs.GetTabControl(sourceIndex);
            if (movedControl is null)
            {
                return;
            }

            var title = sourceTabs.GetTabTitle(sourceIndex);
            sourceTabs.RemoveChild(movedControl);
            targetTabs.AddChild(movedControl);

            var targetIndex = targetTabs.GetTabIdxFromControl(movedControl);
            if (targetIndex >= 0)
            {
                targetTabs.SetTabTitle(targetIndex, title);
                targetTabs.CurrentTab = targetIndex;
            }

            targetHost.HostContainer.Visible = true;
            targetHost.IsClosed = false;

            if (sourceTabs.GetTabCount() == 0)
            {
                sourceHost.HostContainer.Visible = false;
                sourceHost.IsClosed = true;
            }

            UpdateFlexibleHostsLayout();
            RefreshAllMenus();
        }

        private void CollectFlexibleHosts()
        {
            if (_hosts.Count == 0)
            {
                return;
            }

            var parent = _hosts[0].HostContainer.GetParent();
            if (parent is not Control parentControl)
            {
                return;
            }

            var hostSet = new HashSet<Control>();
            foreach (var host in _hosts)
            {
                hostSet.Add(host.HostContainer);
            }

            for (var i = 0; i < parentControl.GetChildCount(); i++)
            {
                if (parentControl.GetChild(i) is not DockingContainerControl panel)
                {
                    continue;
                }

                if (hostSet.Contains(panel))
                {
                    continue;
                }

                _flexibleHosts.Add(new FlexibleHostState(
                    panel,
                    0,
                    0
                ));
            }
        }

        private void UpdateFlexibleHostsLayout()
        {
            var currentLeftInset = ComputeCurrentInset(DockSide.Left);
            var currentRightInset = ComputeCurrentInset(DockSide.Right);

            foreach (var flexible in _flexibleHosts)
            {
                if (!GodotObject.IsInstanceValid(flexible.Control))
                {
                    continue;
                }

                // New DockingHost layout handles sizing; force relayout when host visibility changes.
                if (flexible.Control.GetParent() is DockingHostControl dockingHost)
                {
                    dockingHost.QueueSort();
                }
            }
        }

        private float ComputeCurrentInset(DockSide side)
        {
            var inset = 0f;

            foreach (var host in _hosts)
            {
                if (host.Side != side || host.IsClosed || !GodotObject.IsInstanceValid(host.HostContainer))
                {
                    continue;
                }

                var width = host.BaseWidth > 1f ? host.BaseWidth : EstimateHostWidth(host.HostContainer);
                if (width > inset)
                {
                    inset = width;
                }
            }

            return inset;
        }

        private static DockSide DetermineSide(Control hostContainer)
        {
            if (hostContainer is DockingContainerControl dockingContainer)
            {
                var side = dockingContainer.GetDockSide();
                if (string.Equals(side, "left", StringComparison.OrdinalIgnoreCase))
                {
                    return DockSide.Left;
                }

                if (string.Equals(side, "farleft", StringComparison.OrdinalIgnoreCase))
                {
                    return DockSide.Left;
                }

                if (string.Equals(side, "right", StringComparison.OrdinalIgnoreCase))
                {
                    return DockSide.Right;
                }

                if (string.Equals(side, "farright", StringComparison.OrdinalIgnoreCase))
                {
                    return DockSide.Right;
                }

                return DockSide.Other;
            }

            if (Mathf.IsEqualApprox(hostContainer.AnchorLeft, 0f) && Mathf.IsEqualApprox(hostContainer.AnchorRight, 0f))
            {
                return DockSide.Left;
            }

            if (Mathf.IsEqualApprox(hostContainer.AnchorLeft, 1f) && Mathf.IsEqualApprox(hostContainer.AnchorRight, 1f))
            {
                return DockSide.Right;
            }

            return DockSide.Other;
        }

        private static float EstimateHostWidth(Control hostContainer)
        {
            if (hostContainer is DockingContainerControl dockingContainer)
            {
                return dockingContainer.IsFlex() ? 0f : dockingContainer.GetFixedWidth();
            }

            var width = hostContainer.Size.X;
            if (width > 1f)
            {
                return width;
            }

            var minWidth = hostContainer.CustomMinimumSize.X;
            if (minWidth > 1f)
            {
                return minWidth;
            }

            var left = hostContainer.GetOffset(Side.Left);
            var right = hostContainer.GetOffset(Side.Right);
            var byOffsets = Mathf.Abs(right - left);
            return byOffsets > 1f ? byOffsets : 0f;
        }

        private void RefreshAllMenus()
        {
            _menuActions.Clear();
            long nextId = 1;

            foreach (var host in _hosts)
            {
                if (!GodotObject.IsInstanceValid(host.KebabButton))
                {
                    continue;
                }

                var popup = host.KebabButton.GetPopup();
                popup.Clear();

                var anyClosed = false;
                foreach (var target in _hosts)
                {
                    if (ReferenceEquals(target, host))
                    {
                        continue;
                    }

                    if (!target.IsClosed)
                    {
                        continue;
                    }

                    anyClosed = true;
                    var menuText = $"Move current tab to {target.Name}";
                    var id = nextId++;
                    popup.AddItem(menuText, (int)id);
                    _menuActions[id] = new MenuAction(host, target);
                }

                if (!anyClosed)
                {
                    popup.AddItem("No closed dock hosts", 0);
                    popup.SetItemDisabled(0, true);
                }
            }
        }
    }

    private sealed class DockHostState
    {
        public DockHostState(string name, Control hostContainer, TabContainer tabs)
        {
            Name = name;
            HostContainer = hostContainer;
            Tabs = tabs;
            CanClose = hostContainer is not DockingContainerControl dockingContainer || dockingContainer.IsCloseable();
            IsClosed = !hostContainer.Visible;
        }

        public string Name { get; }
        public Control HostContainer { get; }
        public TabContainer Tabs { get; }
        public MenuButton? KebabButton { get; set; }
        public bool CanClose { get; }
        public bool IsClosed { get; set; }
        public int LastKnownTabCount { get; set; }
        public DockSide Side { get; set; } = DockSide.Other;
        public float BaseWidth { get; set; }
    }

    private sealed class FlexibleHostState
    {
        public FlexibleHostState(Control control, float baseOffsetLeft, float baseOffsetRight)
        {
            Control = control;
            BaseOffsetLeft = baseOffsetLeft;
            BaseOffsetRight = baseOffsetRight;
        }

        public Control Control { get; }
        public float BaseOffsetLeft { get; }
        public float BaseOffsetRight { get; }
    }

    private enum DockSide
    {
        Other,
        Left,
        Right
    }

    private sealed record MenuAction(DockHostState SourceHost, DockHostState TargetHost);
}
