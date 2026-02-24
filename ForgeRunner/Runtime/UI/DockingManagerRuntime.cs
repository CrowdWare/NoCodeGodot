/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

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
        private sealed record PanelMenuBinding(DockingHostControl Host, PopupMenu Popup, int ItemIndex, string PanelId);

        private readonly List<DockHostState> _hosts = [];
        private readonly List<FlexibleHostState> _flexibleHosts = [];
        private readonly Dictionary<string, Button> _dockSelectionButtons = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<PanelMenuBinding> _panelMenuBindings = [];
        private readonly HashSet<ulong> _boundPopupInstanceIds = [];
        private PanelContainer? _dockSelectionDialog;
        private Control? _dockSelectionOverlay;
        private DockHostState? _dockSelectionSourceHost;
        private double _elapsed;
        private float _baseLeftInset;
        private float _baseRightInset;
        public override void _EnterTree()
        {
            AddToGroup("docking_manager_nodes");
            SetProcessUnhandledInput(true);

            // Configure native subwindows only when this node is inside the scene tree.
            if (GetTree()?.Root is { } viewportRoot && viewportRoot.GuiEmbedSubwindows)
            {
                viewportRoot.GuiEmbedSubwindows = false;
            }

            RebuildPanelMenuBindings();
            SyncBoundMenuChecks();

        }

        public void RequestRebuildPanelMenuBindings()
        {
            RebuildPanelMenuBindings();
            SyncBoundMenuChecks();
        }

        public override void _ExitTree()
        {
if (_dockSelectionOverlay is not null && GodotObject.IsInstanceValid(_dockSelectionOverlay))
            {
                _dockSelectionOverlay.QueueFree();
                _dockSelectionOverlay = null;
            }

            if (_dockSelectionDialog is not null && GodotObject.IsInstanceValid(_dockSelectionDialog))
            {
                // Avoid remove_child during tree mutation; deferred free is safe here.
                _dockSelectionDialog.QueueFree();
                _dockSelectionDialog = null;
            }
        }

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

                var closedNow = !host.HostContainer.Visible;
                if (host.IsClosed != closedNow)
                {
                    host.IsClosed = closedNow;
                    changed = true;
                }
            }

            if (changed)
            {
                UpdateFlexibleHostsLayout();
                RefreshAllMenus();
                SyncBoundMenuChecks();
            }
        }

        private void RebuildPanelMenuBindings()
        {
            _panelMenuBindings.Clear();

            if (GetParent() is not Control root)
            {
                return;
            }

            var stack = new Stack<Node>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is PopupMenu popup)
                {
                    BindPopupForPanelToggles(popup);
                }

                for (var i = current.GetChildCount() - 1; i >= 0; i--)
                {
                    stack.Push(current.GetChild(i));
                }
            }
        }

        private void BindPopupForPanelToggles(PopupMenu popup)
        {
            for (var i = 0; i < popup.ItemCount; i++)
            {
                var itemId = popup.GetItemMetadata(i).AsString();
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                if (!TryResolveMappedBinding(itemId, popup, i, out var binding))
                {
                    continue;
                }

                _panelMenuBindings.Add(binding);
            }

            var popupInstanceId = popup.GetInstanceId();
            if (_boundPopupInstanceIds.Contains(popupInstanceId))
            {
                return;
            }

            _boundPopupInstanceIds.Add(popupInstanceId);
            popup.IdPressed += id => HandlePanelMenuPressed(popup, id);
        }

        private void HandlePanelMenuPressed(PopupMenu popup, long id)
        {
            var itemIndex = -1;
            for (var i = 0; i < popup.ItemCount; i++)
            {
                if (popup.GetItemId(i) == id)
                {
                    itemIndex = i;
                    break;
                }
            }

            if (itemIndex < 0)
            {
                return;
            }

            PanelMenuBinding? matched = null;
            foreach (var binding in _panelMenuBindings)
            {
                if (!GodotObject.IsInstanceValid(binding.Popup)
                    || !GodotObject.IsInstanceValid(binding.Host)
                    || binding.ItemIndex < 0
                    || binding.ItemIndex >= binding.Popup.ItemCount)
                {
                    continue;
                }

                if (binding.Popup.GetInstanceId() == popup.GetInstanceId() && binding.ItemIndex == itemIndex)
                {
                    matched = binding;
                    break;
                }
            }

            if (matched is null)
            {
                return;
            }

            var host = matched.Host;
            var panelId = matched.PanelId;
            if (!host.ContainsPanelId(panelId) || !host.TogglePanel(panelId))
            {
                return;
            }

            UpdateFlexibleHostsLayout();
            RefreshAllMenus();
            SyncBoundMenuChecks();
        }

        private bool TryResolveMappedBinding(string menuItemId, PopupMenu popup, int itemIndex, out PanelMenuBinding binding)
        {
            foreach (var hostState in _hosts)
            {
                if (!GodotObject.IsInstanceValid(hostState.Dock)
                    || hostState.Dock.GetParent() is not DockingHostControl host)
                {
                    continue;
                }

                if (!host.TryResolveMappedPanelId(menuItemId, out var panelId)
                    || string.IsNullOrWhiteSpace(panelId)
                    || !host.ContainsPanelId(panelId))
                {
                    continue;
                }

                binding = new PanelMenuBinding(host, popup, itemIndex, panelId);
                return true;
            }

            binding = null!;
            return false;
        }

        private void SyncBoundMenuChecks()
        {
            foreach (var binding in _panelMenuBindings)
            {
                if (!GodotObject.IsInstanceValid(binding.Popup) || !GodotObject.IsInstanceValid(binding.Host))
                {
                    continue;
                }

                if (binding.ItemIndex < 0 || binding.ItemIndex >= binding.Popup.ItemCount)
                {
                    continue;
                }

                var isVisible = binding.Host.IsPanelVisibleById(binding.PanelId);
                if (!binding.Popup.IsItemCheckable(binding.ItemIndex))
                {
                    binding.Popup.SetItemAsCheckable(binding.ItemIndex, true);
                }

                binding.Popup.SetItemChecked(binding.ItemIndex, isVisible);
            }
        }

        private void CreateKebabMenu(DockHostState host)
        {
            var button = host.Dock.EnsureMenuButton();
            host.KebabButton = button;

            var popup = button.GetPopup();
            popup.Clear();

            // We want direct Godot-like docking dialog on kebab click, without popup menu.
            popup.AboutToPopup += () => popup.Hide();
            button.Pressed += () =>
            {
                popup.Hide();
                OpenDockSelectionDialog(host);
            };
        }

        private void OpenDockSelectionDialog(DockHostState sourceHost)
        {
            if (!GodotObject.IsInstanceValid(sourceHost.Dock) || sourceHost.Dock.GetTabCount() <= 0)
            {
                return;
            }

            if (GetTree()?.Root is null)
            {
                return;
            }

            EnsureDockSelectionDialog();
            if (_dockSelectionDialog is null)
            {
                return;
            }

            if (GetParent() is not Control rootControl)
            {
                return;
            }

            // Ensure fullscreen overlay exists and is added before the dialog
            if (_dockSelectionOverlay is null || !GodotObject.IsInstanceValid(_dockSelectionOverlay))
            {
                _dockSelectionOverlay = new Control
                {
                    Name = "DockSelectionOverlay",
                    MouseFilter = Control.MouseFilterEnum.Stop,
                    AnchorsPreset = (int)Control.LayoutPreset.FullRect
                };
                _dockSelectionOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                _dockSelectionOverlay.GuiInput += overlayEvent =>
                {
                    if (overlayEvent is InputEventMouseButton { Pressed: true })
                    {
                        HideDockSelectionDialog();
                    }
                };
            }

            if (_dockSelectionOverlay.GetParent() is null)
            {
                rootControl.AddChild(_dockSelectionOverlay);
            }

            if (_dockSelectionDialog.GetParent() is null)
            {
                rootControl.AddChild(_dockSelectionDialog);
            }

            _dockSelectionOverlay.Visible = true;
            _dockSelectionOverlay.ZIndex = 1999;

            _dockSelectionSourceHost = sourceHost;
            UpdateDockSelectionAvailability(sourceHost);

            var anchorControl = sourceHost.KebabButton is not null && GodotObject.IsInstanceValid(sourceHost.KebabButton)
                ? (Control)sourceHost.KebabButton
                : sourceHost.Dock;

            var anchorRect = anchorControl.GetGlobalRect();

            _dockSelectionDialog.Visible = true;

            var dialogSize = _dockSelectionDialog.Size;
            if (dialogSize.X <= 0 || dialogSize.Y <= 0)
            {
                dialogSize = new Vector2(200, 160);
                _dockSelectionDialog.CustomMinimumSize = dialogSize;
                _dockSelectionDialog.Size = dialogSize;
            }

            // Use canvas/global coordinates here. GlobalPosition/GetGlobalRect are in the same
            // coordinate space as Control.GlobalPosition. Screen coordinates would offset the
            // popup on moved windows and make the menu appear far away from the kebab button.
            var desired = new Vector2(anchorRect.Position.X, anchorRect.Position.Y + anchorRect.Size.Y + 4f);
            var bounds = rootControl.GetGlobalRect();
            var minX = bounds.Position.X;
            var minY = bounds.Position.Y;
            var maxX = Math.Max(minX, bounds.End.X - dialogSize.X);
            var maxY = Math.Max(minY, bounds.End.Y - dialogSize.Y);
            var clamped = new Vector2(
                Mathf.Clamp(desired.X, minX, maxX),
                Mathf.Clamp(desired.Y, minY, maxY));

            _dockSelectionDialog.GlobalPosition = clamped;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_dockSelectionDialog is null
                || !GodotObject.IsInstanceValid(_dockSelectionDialog)
                || !_dockSelectionDialog.Visible)
            {
                return;
            }

            // Escape closes the dialog; clicks outside are handled by the overlay.
            if (@event is InputEventKey keyEvent
                && keyEvent.Pressed
                && !keyEvent.Echo
                && keyEvent.Keycode == Key.Escape)
            {
                HideDockSelectionDialog();
                GetViewport()?.SetInputAsHandled();
            }
        }

        private void HideDockSelectionDialog()
        {
            if (_dockSelectionOverlay is not null && GodotObject.IsInstanceValid(_dockSelectionOverlay))
            {
                _dockSelectionOverlay.Visible = false;
            }

            if (_dockSelectionDialog is null || !GodotObject.IsInstanceValid(_dockSelectionDialog))
            {
                _dockSelectionSourceHost = null;
                return;
            }

            _dockSelectionDialog.Visible = false;
            _dockSelectionSourceHost = null;
        }

        private void EnsureDockSelectionDialog()
        {
            if (_dockSelectionDialog is not null && GodotObject.IsInstanceValid(_dockSelectionDialog))
            {
                return;
            }

            var dialog = new PanelContainer
            {
                Name = "DockSelectionDialog",
                MouseFilter = Control.MouseFilterEnum.Stop,
                FocusMode = Control.FocusModeEnum.All,
                CustomMinimumSize = new Vector2(200, 0),
                Visible = false
            };
            dialog.ZIndex = 2000;

            var dialogStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.14f, 0.15f, 0.18f, 0.98f),
                BorderColor = new Color(0.26f, 0.30f, 0.36f, 1f),
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusBottomLeft = 6,
                ShadowColor = new Color(0f, 0f, 0f, 0.40f),
                ShadowSize = 8,
                ShadowOffset = new Vector2(0, 2),
                ContentMarginLeft = 6,
                ContentMarginTop = 6,
                ContentMarginRight = 6,
                ContentMarginBottom = 6
            };
            dialogStyle.SetBorderWidthAll(1);
            dialog.AddThemeStyleboxOverride("panel", dialogStyle);

            var root = new VBoxContainer
            {
                Name = "Root",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
            };
            root.AddThemeConstantOverride("separation", 4);

            // Header label "Dock Position"
            var headerRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            var arrowLeft = new Label { Text = "<" };
            arrowLeft.AddThemeColorOverride("font_color", new Color(0.6f, 0.65f, 0.72f, 1f));

            var titleLabel = new Label
            {
                Name = "Title",
                Text = "Dock Position",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            titleLabel.AddThemeColorOverride("font_color", new Color(0.87f, 0.91f, 0.98f, 1f));

            var arrowRight = new Label { Text = ">" };
            arrowRight.AddThemeColorOverride("font_color", new Color(0.6f, 0.65f, 0.72f, 1f));

            headerRow.AddChild(arrowLeft);
            headerRow.AddChild(titleLabel);
            headerRow.AddChild(arrowRight);

            // Dock grid
            var dockGrid = new HBoxContainer
            {
                Name = "Grid",
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
            };
            dockGrid.AddThemeConstantOverride("separation", 3);
            dockGrid.AddChild(CreateDockColumn("farleft", "farleftbottom"));
            dockGrid.AddChild(CreateDockColumn("left", "leftbottom"));
            dockGrid.AddChild(CreateDockSlotButton("center", center: true));
            dockGrid.AddChild(CreateDockColumn("right", "rightbottom"));
            dockGrid.AddChild(CreateDockColumn("farright", "farrightbottom"));

            // Separator
            var separator = new HSeparator
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            // Action buttons (vertical, full-width, like Godot)
            var floatingButton = new Button
            {
                Text = "  Make Floating",
                Flat = false,
                Alignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            floatingButton.Pressed += () =>
            {
                var sourceHost = _dockSelectionSourceHost;
                if (sourceHost is null)
                {
                    return;
                }

                HideDockSelectionDialog();
                ExecuteUndockAction(sourceHost);
            };

            var closeButton = new Button
            {
                Text = "  Close Panel",
                Flat = false,
                Alignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            closeButton.Pressed += () =>
            {
                var sourceHost = _dockSelectionSourceHost;
                if (sourceHost is null)
                {
                    return;
                }

                ExecuteCloseAction(sourceHost);
                HideDockSelectionDialog();
            };

            // Style close button with an X prefix
            var closePrefix = new Label { Text = "✕" };

            root.AddChild(headerRow);
            root.AddChild(dockGrid);
            root.AddChild(separator);
            root.AddChild(floatingButton);
            root.AddChild(closeButton);
            dialog.AddChild(root);

            dialog.VisibilityChanged += () =>
            {
                if (dialog.Visible)
                {
                    return;
                }

                _dockSelectionSourceHost = null;
                foreach (var button in _dockSelectionButtons.Values)
                {
                    if (GodotObject.IsInstanceValid(button))
                    {
                        button.Visible = true;
                        button.Disabled = false;
                    }
                }
            };

            _dockSelectionDialog = dialog;
        }

        private VBoxContainer CreateDockColumn(string topPosition, string bottomPosition)
        {
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(36, 0)
            };
            column.AddThemeConstantOverride("separation", 3);

            column.AddChild(CreateDockSlotButton(topPosition));
            column.AddChild(CreateDockSlotButton(bottomPosition));
            return column;
        }

        private Button CreateDockSlotButton(string position, bool center = false)
        {
            var button = new Button
            {
                Text = string.Empty,
                SizeFlagsHorizontal = center ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkCenter,
                SizeFlagsVertical = center ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ExpandFill,
                CustomMinimumSize = center ? new Vector2(44, 0) : new Vector2(34, 26)
            };
            button.Flat = false;
            button.AddThemeColorOverride("font_color", new Color(0.96f, 0.96f, 0.98f, 1f));
            button.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f, 1f));

            var normal = new StyleBoxFlat
            {
                BgColor = center
                    ? new Color(0.34f, 0.36f, 0.42f, 1f)
                    : new Color(0.24f, 0.24f, 0.28f, 1f),
                CornerRadiusTopLeft = 5,
                CornerRadiusTopRight = 5,
                CornerRadiusBottomLeft = 5,
                CornerRadiusBottomRight = 5
            };
            normal.SetBorderWidthAll(0);

            var hover = new StyleBoxFlat
            {
                BgColor = center
                    ? new Color(0.40f, 0.42f, 0.50f, 1f)
                    : new Color(0.30f, 0.32f, 0.38f, 1f),
                CornerRadiusTopLeft = 5,
                CornerRadiusTopRight = 5,
                CornerRadiusBottomLeft = 5,
                CornerRadiusBottomRight = 5
            };
            hover.SetBorderWidthAll(0);

            var pressed = new StyleBoxFlat
            {
                BgColor = center
                    ? new Color(0.30f, 0.44f, 0.68f, 1f)
                    : new Color(0.20f, 0.34f, 0.56f, 1f),
                CornerRadiusTopLeft = 5,
                CornerRadiusTopRight = 5,
                CornerRadiusBottomLeft = 5,
                CornerRadiusBottomRight = 5
            };
            pressed.SetBorderWidthAll(0);

            var disabled = new StyleBoxFlat
            {
                BgColor = center
                    ? new Color(0.26f, 0.28f, 0.34f, 0.95f)
                    : new Color(0.28f, 0.30f, 0.34f, 0.95f),
                CornerRadiusTopLeft = 5,
                CornerRadiusTopRight = 5,
                CornerRadiusBottomLeft = 5,
                CornerRadiusBottomRight = 5
            };
            disabled.SetBorderWidthAll(0);

            button.AddThemeStyleboxOverride("normal", normal);
            button.AddThemeStyleboxOverride("hover", hover);
            button.AddThemeStyleboxOverride("pressed", pressed);
            button.AddThemeStyleboxOverride("disabled", disabled);
            button.TooltipText = position;
            _dockSelectionButtons[position] = button;

            button.Pressed += () =>
            {
                var sourceHost = _dockSelectionSourceHost;
                if (sourceHost is null)
                {
                    return;
                }

                ExecuteDockSelection(sourceHost, position);
                HideDockSelectionDialog();
            };

            return button;
        }

        private void UpdateDockSelectionAvailability(DockHostState sourceHost)
        {
            foreach (var entry in _dockSelectionButtons)
            {
                if (!GodotObject.IsInstanceValid(entry.Value))
                {
                    continue;
                }

                var isCenter = string.Equals(entry.Key, "center", StringComparison.OrdinalIgnoreCase);
                var canDock = CanDockToPosition(sourceHost, entry.Key);

                if (isCenter)
                {
                    // Center is always visible as orientation landmark; disable if not reachable.
                    entry.Value.Visible = true;
                    entry.Value.Disabled = !canDock;
                }
                else
                {
                    // Side slots are hidden when not reachable to keep the dialog compact.
                    entry.Value.Visible = canDock;
                    entry.Value.Disabled = false;
                }
            }
        }

        private static bool CanDockToPosition(DockHostState sourceHost, string position)
        {
            var target = FindTargetDockContainer(sourceHost, position);
            if (target is null)
            {
                return false;
            }

            if (!target.IsDragToRearrangeEnabled())
            {
                return false;
            }

            return sourceHost.Dock.GetTabsRearrangeGroup() == target.GetTabsRearrangeGroup();
        }

        private static DockingContainerControl? FindTargetDockContainer(DockHostState sourceHost, string position)
        {
            if (sourceHost.Dock.GetParent() is not DockingHostControl dockingHost)
            {
                return null;
            }

            for (var i = 0; i < dockingHost.GetChildCount(); i++)
            {
                if (dockingHost.GetChild(i) is not DockingContainerControl target)
                {
                    continue;
                }

                if (string.Equals(target.GetDockSide(), position, StringComparison.OrdinalIgnoreCase))
                {
                    return target;
                }
            }

            return null;
        }

        private void ExecuteDockSelection(DockHostState sourceHost, string position)
        {
            if (!GodotObject.IsInstanceValid(sourceHost.Dock))
            {
                return;
            }

            if (!CanDockToPosition(sourceHost, position))
            {
                return;
            }

            if (sourceHost.Dock.GetTabCount() <= 0)
            {
                return;
            }

            var dockingHost = sourceHost.Dock.GetParent() as DockingHostControl;
            if (dockingHost is null)
            {
                return;
            }

            var index = sourceHost.Dock.GetCurrentTab();
            if (index < 0 || index >= sourceHost.Dock.GetTabCount())
            {
                index = 0;
            }

            var panel = sourceHost.Dock.GetTabControl(index);
            if (panel is null)
            {
                return;
            }

            if (!dockingHost.DockPanel(panel, position))
            {
                return;
            }

            if (sourceHost.Dock.GetTabCount() == 0)
            {
                sourceHost.HostContainer.Visible = false;
                sourceHost.IsClosed = true;
            }

            var targetHost = FindHostByDockSide(position);
            if (targetHost is not null)
            {
                targetHost.HostContainer.Visible = true;
                targetHost.IsClosed = false;
            }

            UpdateFlexibleHostsLayout();
            RefreshAllMenus();
            SyncBoundMenuChecks();
        }

        private void ExecuteCloseAction(DockHostState sourceHost)
        {
            if (!GodotObject.IsInstanceValid(sourceHost.Dock))
            {
                return;
            }

            if (sourceHost.Dock.GetTabCount() <= 0)
            {
                return;
            }

            var index = sourceHost.Dock.GetCurrentTab();
            if (index < 0 || index >= sourceHost.Dock.GetTabCount())
            {
                index = 0;
            }

            var panel = sourceHost.Dock.GetTabControl(index);
            if (panel is null)
            {
                return;
            }

            var panelId = ResolvePanelId(panel);
            var dockingHost = sourceHost.Dock.GetParent() as DockingHostControl;
            if (!string.IsNullOrWhiteSpace(panelId) && dockingHost is not null)
            {
                dockingHost.HidePanelById(panelId);
            }
            else
            {
                sourceHost.Dock.RemoveDockTab(panel);
            }

            if (sourceHost.Dock.GetTabCount() == 0)
            {
                sourceHost.HostContainer.Visible = false;
                sourceHost.IsClosed = true;
            }

            UpdateFlexibleHostsLayout();
            RefreshAllMenus();
            SyncBoundMenuChecks();
        }

        private DockHostState? FindHostByDockSide(string position)
        {
            foreach (var host in _hosts)
            {
                if (!GodotObject.IsInstanceValid(host.Dock))
                {
                    continue;
                }

                if (string.Equals(host.Dock.GetDockSide(), position, StringComparison.OrdinalIgnoreCase))
                {
                    return host;
                }
            }

            return null;
        }

        private static string ResolvePanelId(Control panel)
        {
            if (panel.HasMeta(NodePropertyMapper.MetaId))
            {
                var id = panel.GetMeta(NodePropertyMapper.MetaId).AsString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    return id;
                }
            }

            return panel.Name;
        }

        private void ExecuteUndockAction(DockHostState sourceHost)
        {
            if (!GodotObject.IsInstanceValid(sourceHost.Dock))
            {
                return;
            }

            var sourceDock = sourceHost.Dock;
            if (sourceDock.GetTabCount() <= 0)
            {
                return;
            }

            var sourceIndex = sourceDock.GetCurrentTab();
            if (sourceIndex < 0 || sourceIndex >= sourceDock.GetTabCount())
            {
                sourceIndex = 0;
            }

            var panel = sourceDock.GetTabControl(sourceIndex);
            if (panel is null)
            {
                return;
            }

            var panelTitle = sourceDock.GetTabTitle(sourceIndex);
            if (string.IsNullOrWhiteSpace(panelTitle))
            {
                panelTitle = string.IsNullOrWhiteSpace(panel.Name) ? "Floating Panel" : panel.Name;
            }

            var globalRect = sourceDock.GetGlobalRect();
            var screenPos = sourceDock.GetScreenPosition();
            sourceDock.RemoveDockTab(panel);

            if (sourceDock.GetTabCount() == 0)
            {
                sourceHost.HostContainer.Visible = false;
                sourceHost.IsClosed = true;
            }

            var width = Math.Max(220, Mathf.RoundToInt(globalRect.Size.X));
            var height = Math.Max(140, Mathf.RoundToInt(globalRect.Size.Y));
            // Window positions are screen-space. Use screen-position of the source dock,
            // not viewport-local global rect coordinates.
            var posX = Mathf.RoundToInt(screenPos.X);
            var posY = Mathf.RoundToInt(screenPos.Y);

            var window = new Window
            {
                Name = $"Undocked_{sourceDock.Name}_{panel.Name}",
                Title = panelTitle,
                InitialPosition = Window.WindowInitialPosition.Absolute,
                Position = new Vector2I(posX, posY),
                Size = new Vector2I(width, height),
                MinSize = new Vector2I(Math.Min(width, 220), Math.Min(height, 140)),
                Visible = false,
                Transient = false,
                Unresizable = false
            };

            panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            window.AddChild(panel);

            if (GetTree()?.Root is null)
            {
                sourceDock.AddDockTab(panel, panelTitle);
                sourceHost.HostContainer.Visible = true;
                sourceHost.IsClosed = false;
                RefreshAllMenus();
                return;
            }

            // Safety: if embedding was switched externally, enforce native window mode.
            if (GetTree()!.Root.GuiEmbedSubwindows)
            {
                GetTree()!.Root.GuiEmbedSubwindows = false;
            }

            GetTree()!.Root.AddChild(window);
            RunnerLogger.Info("UI", $"Opened floating window '{window.Name}' for panel '{panelTitle}'.");
            window.CloseRequested += () =>
            {
                if (panel.GetParent() == window)
                {
                    window.RemoveChild(panel);
                }

                if (GodotObject.IsInstanceValid(sourceDock))
                {
                    sourceDock.AddDockTab(panel, panelTitle);
                    var tabIndex = sourceDock.GetTabIndexFromControl(panel);
                    sourceDock.SetCurrentTab(tabIndex >= 0 ? tabIndex : 0);
                    sourceHost.HostContainer.Visible = true;
                    sourceHost.IsClosed = false;
                }

                UpdateFlexibleHostsLayout();
                RefreshAllMenus();
                window.QueueFree();
            };

            window.Show();
            UpdateFlexibleHostsLayout();
            RefreshAllMenus();
            SyncBoundMenuChecks();
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
            foreach (var host in _hosts)
            {
                if (!GodotObject.IsInstanceValid(host.KebabButton))
                {
                    continue;
                }

                host.KebabButton.Visible = host.HostContainer.Visible && HasCompatibleDockingPeer(host);
                var popup = host.KebabButton.GetPopup();
                popup.Clear();
            }
        }

        private bool HasCompatibleDockingPeer(DockHostState sourceHost)
        {
            if (!GodotObject.IsInstanceValid(sourceHost.Dock) || !sourceHost.Dock.IsDragToRearrangeEnabled())
            {
                return false;
            }

            var sourceGroup = sourceHost.Dock.GetTabsRearrangeGroup();
            foreach (var candidate in _hosts)
            {
                if (ReferenceEquals(candidate, sourceHost)
                    || !GodotObject.IsInstanceValid(candidate.Dock)
                    || !candidate.Dock.IsDragToRearrangeEnabled())
                {
                    continue;
                }

                if (candidate.Dock.GetTabsRearrangeGroup() != sourceGroup)
                {
                    continue;
                }

                return true;
            }

            return false;
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


    }  // end DockingManagerNode

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

}
