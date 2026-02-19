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

            if (current is DockingContainerControl dockingContainer)
            {
                if (!scopedMode || dockingContainer.GetParent() is DockingHostControl)
                {
                    dockingContainer.EnsureDockStructure();
                    var dragToRearrangeEnabled = dockingContainer.IsDragToRearrangeEnabled();

                    if (!dragToRearrangeEnabled)
                    {
                        continue;
                    }

                    var hostName = ResolveHostName(dockingContainer, result.Count + 1);
                    result.Add(new DockHostState(hostName, dockingContainer));
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

    private static string ResolveHostName(Control hostContainer, int fallbackIndex)
    {
        if (hostContainer is DockingContainerControl dockingContainer)
        {
            return GetDockSideLabel(dockingContainer.GetDockSideKind());
        }

        if (!string.IsNullOrWhiteSpace(hostContainer.Name))
        {
            return hostContainer.Name;
        }

        return $"dockHost{fallbackIndex}";
    }

    private static string GetDockSideLabel(DockingContainerControl.DockSideKind side)
    {
        return side switch
        {
            DockingContainerControl.DockSideKind.FarLeft => "far left",
            DockingContainerControl.DockSideKind.FarLeftBottom => "far left bottom",
            DockingContainerControl.DockSideKind.Left => "left",
            DockingContainerControl.DockSideKind.LeftBottom => "left bottom",
            DockingContainerControl.DockSideKind.Right => "right",
            DockingContainerControl.DockSideKind.RightBottom => "right bottom",
            DockingContainerControl.DockSideKind.FarRight => "far right",
            DockingContainerControl.DockSideKind.FarRightBottom => "far right bottom",
            _ => "center"
        };
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
                    || !GodotObject.IsInstanceValid(host.Dock))
                {
                    continue;
                }

                if (host.HostContainer.Visible && host.Dock.GetTabCount() == 0)
                {
                    // Empty hosts should be auto-hidden regardless of closeable flag.
                    host.HostContainer.Visible = false;
                    host.IsClosed = true;
                    changed = true;
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
            var button = host.Dock.EnsureMenuButton();
            host.KebabButton = button;

            var popup = button.GetPopup();
            popup.Clear();
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
            if (!GodotObject.IsInstanceValid(sourceHost.Dock) || !GodotObject.IsInstanceValid(targetHost.Dock))
            {
                return;
            }

            if (!CanMoveBetween(sourceHost, targetHost))
            {
                return;
            }

            var sourceDock = sourceHost.Dock;
            var targetDock = targetHost.Dock;

            if (sourceDock.GetTabCount() <= 0)
            {
                targetHost.HostContainer.Visible = true;
                targetHost.IsClosed = false;
                RefreshAllMenus();
                return;
            }

            var sourceIndex = sourceDock.GetCurrentTab();
            if (sourceIndex < 0 || sourceIndex >= sourceDock.GetTabCount())
            {
                sourceIndex = 0;
            }

            var movedControl = sourceDock.GetTabControl(sourceIndex);
            if (movedControl is null)
            {
                return;
            }

            var title = sourceDock.GetTabTitle(sourceIndex);
            sourceDock.RemoveDockTab(movedControl);
            targetDock.AddDockTab(movedControl, title);

            var targetIndex = targetDock.GetTabIndexFromControl(movedControl);
            targetDock.SetCurrentTab(targetIndex >= 0 ? targetIndex : 0);

            targetHost.HostContainer.Visible = true;
            targetHost.IsClosed = false;

            if (sourceDock.GetTabCount() == 0)
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
                var side = dockingContainer.GetDockSideKind();
                if (side is DockingContainerControl.DockSideKind.Left
                    or DockingContainerControl.DockSideKind.LeftBottom
                    or DockingContainerControl.DockSideKind.FarLeft
                    or DockingContainerControl.DockSideKind.FarLeftBottom)
                {
                    return DockSide.Left;
                }

                if (side is DockingContainerControl.DockSideKind.Right
                    or DockingContainerControl.DockSideKind.RightBottom
                    or DockingContainerControl.DockSideKind.FarRight
                    or DockingContainerControl.DockSideKind.FarRightBottom)
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

                host.KebabButton.Visible = host.HostContainer.Visible;
                var popup = host.KebabButton.GetPopup();
                popup.Clear();
                foreach (var target in _hosts)
                {
                    if (ReferenceEquals(target, host))
                    {
                        continue;
                    }

                    if (!CanMoveBetween(host, target))
                    {
                        continue;
                    }

                    var menuText = $"Move to {target.Name}";
                    var id = nextId++;
                    popup.AddItem(menuText, (int)id);
                    _menuActions[id] = new MenuAction(host, target);
                }

                if (popup.ItemCount == 0)
                {
                    popup.AddItem("No target", 0);
                    popup.SetItemDisabled(0, true);
                }
            }
        }

        private static bool CanMoveBetween(DockHostState source, DockHostState target)
        {
            if (target.Slot == source.Slot)
            {
                return false;
            }

            if (!target.Dock.IsDragToRearrangeEnabled())
            {
                return false;
            }

            return source.Dock.GetTabsRearrangeGroup() == target.Dock.GetTabsRearrangeGroup();
        }
    }

    private sealed class DockHostState
    {
        public DockHostState(string name, DockingContainerControl dock)
        {
            Name = name;
            Dock = dock;
            HostContainer = dock;
            Slot = dock.GetDockSideKind();
            CanClose = dock.IsCloseable();
            IsClosed = !dock.Visible;
        }

        public string Name { get; }
        public DockingContainerControl Dock { get; }
        public Control HostContainer { get; }
        public DockingContainerControl.DockSideKind Slot { get; }
        public MenuButton? KebabButton { get; set; }
        public bool CanClose { get; }
        public bool IsClosed { get; set; }
        public DockSide Side { get; set; } = DockSide.Other;
        public float BaseWidth { get; set; }
        public Vector2 LastKnownSize { get; set; }
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
