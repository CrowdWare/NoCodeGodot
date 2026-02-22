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

namespace Runtime.UI;

public partial class DocumentScrollContainer : ScrollContainer
{
    private bool _alwaysVisible = true;
    private bool _visibleOnScroll;
    private int _fadeOutMs;
    private string _position = "right";
    private double _hideAtSeconds = -1;

    public void ConfigureFromSmlMeta(Control source)
    {
        if (source.HasMeta(NodePropertyMapper.MetaScrollbarVisible))
        {
            _alwaysVisible = source.GetMeta(NodePropertyMapper.MetaScrollbarVisible).AsBool();
        }

        if (source.HasMeta(NodePropertyMapper.MetaScrollbarVisibleOnScroll))
        {
            _visibleOnScroll = source.GetMeta(NodePropertyMapper.MetaScrollbarVisibleOnScroll).AsBool();
        }

        if (source.HasMeta(NodePropertyMapper.MetaScrollbarFadeOutTime))
        {
            _fadeOutMs = source.GetMeta(NodePropertyMapper.MetaScrollbarFadeOutTime).AsInt32();
        }

        if (source.HasMeta(NodePropertyMapper.MetaScrollbarPosition))
        {
            _position = source.GetMeta(NodePropertyMapper.MetaScrollbarPosition).AsString().ToLowerInvariant();
        }

        if (source.HasMeta(NodePropertyMapper.MetaScrollbarWidth))
        {
            AddThemeConstantOverride("v_scroll_bar_min_size", source.GetMeta(NodePropertyMapper.MetaScrollbarWidth).AsInt32());
        }

        if (source.HasMeta(NodePropertyMapper.MetaScrollbarHeight))
        {
            AddThemeConstantOverride("h_scroll_bar_min_size", source.GetMeta(NodePropertyMapper.MetaScrollbarHeight).AsInt32());
        }
    }

    public override void _Ready()
    {
        var h = GetHScrollBar();
        var v = GetVScrollBar();

        h.ValueChanged += _ => OnScrollInteraction();
        v.ValueChanged += _ => OnScrollInteraction();

        ApplyVisibilityState(forceVisible: _alwaysVisible || !_visibleOnScroll);

        if (_position is "left" or "top")
        {
            RunnerLogger.Warn("UI", $"scrollBarPosition='{_position}' is currently best-effort; Godot places native scrollbars right/bottom.");
        }
    }

    public override void _Process(double delta)
    {
        if (!_visibleOnScroll || _alwaysVisible)
        {
            return;
        }

        if (_hideAtSeconds > 0 && Time.GetTicksMsec() / 1000.0 >= _hideAtSeconds)
        {
            ApplyVisibilityState(forceVisible: false);
            _hideAtSeconds = -1;
        }
    }

    private void OnScrollInteraction()
    {
        if (!_visibleOnScroll || _alwaysVisible)
        {
            return;
        }

        ApplyVisibilityState(forceVisible: true);
        _hideAtSeconds = Time.GetTicksMsec() / 1000.0 + Math.Max(0, _fadeOutMs) / 1000.0;
    }

    private void ApplyVisibilityState(bool forceVisible)
    {
        var h = GetHScrollBar();
        var v = GetVScrollBar();

        if (_alwaysVisible)
        {
            h.Visible = true;
            v.Visible = true;
        }
        else if (_visibleOnScroll)
        {
            h.Visible = forceVisible;
            v.Visible = forceVisible;
        }

        RunnerLogger.Info(
            "UI",
            $"[scroll] visibleH={h.Visible}, visibleV={v.Visible}, alwaysVisible={_alwaysVisible}, visibleOnScroll={_visibleOnScroll}, fadeOutMs={_fadeOutMs}, position={_position}"
        );
    }
}
