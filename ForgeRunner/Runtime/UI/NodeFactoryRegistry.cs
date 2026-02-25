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
using Runtime.ThreeD;
using System;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed class NodeFactoryRegistry
{
    private readonly Dictionary<string, Func<Control>> _factories = new(StringComparer.OrdinalIgnoreCase);

    public NodeFactoryRegistry()
    {
        RegisterDefaults();
    }

    public void Register(string smlNodeName, Func<Control> factory)
    {
        _factories[smlNodeName] = factory;
    }

    public bool TryCreate(string smlNodeName, out Control control)
    {
        if (_factories.TryGetValue(smlNodeName, out var factory))
        {
            control = factory();
            return true;
        }

        control = null!;
        return false;
    }

    public IEnumerable<string> GetRegisteredNodeNames()
    {
        return _factories.Keys;
    }

    private void RegisterDefaults()
    {
        Register("Window", () => new Panel());
        Register("SplashScreen", () => new Panel());
        Register("Panel", () => new Panel());
        Register("WindowDrag", () => new WindowDragControl());
        Register("PanelContainer", () => new PanelContainer());
        Register("DockingHost", () => new DockingHostControl());
        Register("DockingContainer", () => new DockingContainerControl());
        Register("Label", () => new Label());
        Register("Button", () => new Button());
        Register("TextEdit", () => new TextEdit());
        Register("CodeEdit", () => new CodeEdit());
        Register("HBoxContainer", () => new HBoxContainer());
        Register("VBoxContainer", () => new VBoxContainer());
        Register("Markdown", () => new VBoxContainer());
        Register("MarkdownLabel", () => new RichTextLabel
        {
            FitContent = true,
            ScrollActive = false
        });
        Register("Image", () => new TextureRect());
        Register("Control", () => new Control());
        Register("TabBar", () => new TabBar());
        Register("TabContainer", () => new TabContainer());
        Register("MenuBar", () => new MenuBar());
        Register("HSlider", () => new HSlider());
        Register("ProgressBar", () => new ProgressBar());
        Register("Tree", () => new Tree());
        Register("Slider", () => new HSlider());
        Register("Video", () => new VideoStreamPlayer());
        Register("Viewport3D", () => new Viewport3DControl());
    }
}
