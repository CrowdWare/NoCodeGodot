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
using System;
using System.Collections.Generic;

namespace Runtime.ThreeD;

// ────────────────────────────────────────────────────────────────────────────
// TimelineTrackArea — custom drawing canvas for tracks, keyframes, playhead
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Custom Control that draws the timeline ruler, per-bone tracks,
/// keyframe diamonds, and the draggable playhead.
/// Owned by <see cref="TimelineControl"/>.
/// </summary>
public sealed partial class TimelineTrackArea : Control
{
    // ── Visual layout constants ───────────────────────────────────────────
    internal const float RulerHeight   = 20f;
    internal const float TrackHeight   = 22f;
    internal const float BoneNameWidth = 120f;
    internal const float PixPerFrame   = 8f;

    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColBg        = new(0.14f, 0.14f, 0.14f);
    private static readonly Color ColTrackAlt  = new(0.11f, 0.11f, 0.11f);
    private static readonly Color ColRulerBg   = new(0.20f, 0.20f, 0.20f);
    private static readonly Color ColTick      = new(0.60f, 0.60f, 0.60f);
    private static readonly Color ColBoneName  = new(0.82f, 0.82f, 0.82f);
    private static readonly Color ColKeyframe  = new(0.95f, 0.78f, 0.10f);
    private static readonly Color ColPlayhead  = new(1.00f, 0.30f, 0.20f);
    private static readonly Color ColSeparator = new(0.28f, 0.28f, 0.28f, 0.50f);

    // ── Owner reference ───────────────────────────────────────────────────
    internal new TimelineControl? Owner;
    internal float ScrollOffset;

    private bool _dragging;

    public TimelineTrackArea() { FocusMode = FocusModeEnum.Click; }

    // ── Drawing ───────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (Owner is null) return;

        var w     = Size.X;
        var h     = Size.Y;
        var font  = ThemeDB.FallbackFont;
        var fSize = (int)ThemeDB.FallbackFontSize;

        // Background
        DrawRect(new Rect2(0, 0, w, h), ColBg);

        // Ruler strip
        DrawRect(new Rect2(BoneNameWidth, 0, w - BoneNameWidth, RulerHeight), ColRulerBg);

        // Ruler ticks and labels
        var step = TickStep(Owner.TotalFrames);
        for (var f = 0; f <= Owner.TotalFrames; f += step)
        {
            var x = BoneNameWidth + f * PixPerFrame - ScrollOffset;
            if (x < BoneNameWidth || x > w) continue;
            DrawLine(new Vector2(x, RulerHeight - 5f), new Vector2(x, RulerHeight), ColTick);
            DrawString(font, new Vector2(x + 2f, RulerHeight - 6f), f.ToString(),
                HorizontalAlignment.Left, -1, fSize - 1, ColTick);
        }

        // Column / ruler separators
        DrawLine(new Vector2(BoneNameWidth, 0), new Vector2(BoneNameWidth, h), ColSeparator);
        DrawLine(new Vector2(0, RulerHeight),   new Vector2(w, RulerHeight),   ColSeparator);

        // ── Bone tracks ───────────────────────────────────────────────────
        var bones = Owner.TrackedBones;
        for (var row = 0; row < bones.Count; row++)
        {
            var y = RulerHeight + row * TrackHeight;

            if (row % 2 == 1)
                DrawRect(new Rect2(0, y, w, TrackHeight), ColTrackAlt);

            // Bone name (left strip)
            DrawString(font, new Vector2(4f, y + TrackHeight * 0.5f + fSize * 0.35f),
                DisplayName(bones[row]),
                HorizontalAlignment.Left, (int)(BoneNameWidth - 8), fSize, ColBoneName);

            // Keyframe diamonds
            foreach (var frame in Owner.GetKeyframesForBone(bones[row]))
            {
                var kx = BoneNameWidth + frame * PixPerFrame - ScrollOffset;
                if (kx < BoneNameWidth - 8f || kx > w + 8f) continue;
                DrawDiamond(kx, y + TrackHeight * 0.5f, 5f, ColKeyframe);
            }

            DrawLine(new Vector2(0, y + TrackHeight), new Vector2(w, y + TrackHeight), ColSeparator);
        }

        // ── Playhead ──────────────────────────────────────────────────────
        var phX = BoneNameWidth + Owner.CurrentFrame * PixPerFrame - ScrollOffset;
        if (phX >= BoneNameWidth - 1f && phX <= w + 1f)
        {
            DrawLine(new Vector2(phX, 0f), new Vector2(phX, h), ColPlayhead, 2f);
            // Triangle handle at top
            DrawPolygon(
                new Vector2[] { new(phX - 5f, 0f), new(phX + 5f, 0f), new(phX, 9f) },
                new Color[]   { ColPlayhead });
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event)
    {
        if (Owner is null) return;

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            _dragging = mb.Pressed;
            if (mb.Pressed) Seek(mb.Position.X);
            AcceptEvent();
        }
        else if (@event is InputEventMouseMotion mm
                 && _dragging
                 && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            Seek(mm.Position.X);
            AcceptEvent();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void Seek(float mouseX)
    {
        if (Owner is null) return;
        var frame = Mathf.Clamp(
            Mathf.RoundToInt((mouseX - BoneNameWidth + ScrollOffset) / PixPerFrame),
            0, Owner.TotalFrames);
        Owner.SetCurrentFrame(frame);
    }

    private void DrawDiamond(float cx, float cy, float hs, Color color)
    {
        DrawPolygon(
            new Vector2[] { new(cx, cy - hs), new(cx + hs, cy), new(cx, cy + hs), new(cx - hs, cy) },
            new Color[]   { color });
    }

    /// <summary>Shorten long bone names for the track label (e.g. "mixamorig_Head" → "Head").</summary>
    private static string DisplayName(string name)
    {
        if (name.Length <= 16) return name;
        var i = name.LastIndexOf('_');
        if (i < 0) i = name.LastIndexOf(':');
        return i >= 0 ? name[(i + 1)..] : name;
    }

    private static int TickStep(int total) => total switch
    {
        <= 48  => 4,
        <= 120 => 10,
        <= 300 => 25,
        _      => 50,
    };
}

// ────────────────────────────────────────────────────────────────────────────
// TimelineControl — the full timeline widget exposed via SML
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A Forge control for keyframe-based animation editing.
///
/// SML usage:
/// <code>
/// Timeline {
///     fps: 24
///     totalFrames: 120
/// }
/// </code>
/// </summary>
public sealed partial class TimelineControl : Control
{
    // ── SML-settable properties ───────────────────────────────────────────
    public int Fps         { get; set; } = 24;
    public int TotalFrames { get; set; } = 120;

    // ── Accessors used by TimelineTrackArea ───────────────────────────────
    public int                   CurrentFrame => _currentFrame;
    public IReadOnlyList<string> TrackedBones => _trackedBones;

    /// <summary>Returns the frame indices that have a keyframe for the given bone.</summary>
    public IEnumerable<int> GetKeyframesForBone(string boneName)
    {
        foreach (var (frame, pose) in _keyframes)
            if (pose.ContainsKey(boneName))
                yield return frame;
    }

    // ── Scene nodes ───────────────────────────────────────────────────────
    private readonly Button            _btnPlayPause;
    private readonly Button            _btnStop;
    private readonly Label             _frameLabel;
    private readonly TimelineTrackArea _trackArea;
    private readonly HScrollBar        _scrollBar;

    // ── Keyframe data: frame → (boneName → rotation) ──────────────────────
    private readonly SortedDictionary<int, Dictionary<string, Quaternion>> _keyframes = new();
    private readonly List<string> _trackedBones = [];

    // ── Playback state ────────────────────────────────────────────────────
    private int   _currentFrame;
    private bool  _isPlaying;
    private float _playAccumulated;

    // ── Events ────────────────────────────────────────────────────────────
    public event Action<int>? FrameChanged;
    public event Action?      PlaybackStarted;
    public event Action?      PlaybackStopped;

    // ─────────────────────────────────────────────────────────────────────
    public TimelineControl()
    {
        CustomMinimumSize   = new Vector2(400, 110);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // ── Toolbar ───────────────────────────────────────────────────────
        _btnPlayPause = new Button { Name = "BtnPlay", Text = "▶", CustomMinimumSize = new Vector2(36, 28) };
        _btnStop      = new Button { Name = "BtnStop", Text = "■", CustomMinimumSize = new Vector2(36, 28) };
        _frameLabel   = new Label  { Name = "FrameLabel", Text = "0 / 120", SizeFlagsVertical = SizeFlags.ShrinkCenter };

        var toolbar = new HBoxContainer { Name = "Toolbar", CustomMinimumSize = new Vector2(0, 36) };
        toolbar.AddChild(_btnPlayPause);
        toolbar.AddChild(_btnStop);
        toolbar.AddChild(new Control { CustomMinimumSize = new Vector2(8, 0) }); // gap
        toolbar.AddChild(_frameLabel);

        _btnPlayPause.Pressed += OnPlayPausePressed;
        _btnStop.Pressed      += OnStopPressed;

        // ── Track area ────────────────────────────────────────────────────
        _trackArea = new TimelineTrackArea
        {
            Name                = "TrackArea",
            Owner               = this,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical   = SizeFlags.ExpandFill,
        };
        _trackArea.Resized += RefreshScrollRange;

        // ── Scroll bar ────────────────────────────────────────────────────
        _scrollBar = new HScrollBar { Name = "ScrollBar", CustomMinimumSize = new Vector2(0, 16) };
        _scrollBar.ValueChanged += v =>
        {
            _trackArea.ScrollOffset = (float)v;
            _trackArea.QueueRedraw();
        };

        // ── Layout ────────────────────────────────────────────────────────
        var root = new VBoxContainer
        {
            Name         = "Root",
            AnchorLeft   = 0f, AnchorRight  = 1f,
            AnchorTop    = 0f, AnchorBottom = 1f,
            OffsetLeft   = 0,  OffsetRight  = 0,
            OffsetTop    = 0,  OffsetBottom = 0,
        };
        root.AddChild(toolbar);
        root.AddChild(_trackArea);
        root.AddChild(_scrollBar);
        AddChild(root);
    }

    // ── Godot lifecycle ───────────────────────────────────────────────────

    public override void _Ready()
    {
        UpdateFrameLabel();
        RefreshScrollRange();
    }

    public override void _Process(double delta)
    {
        if (!_isPlaying) return;

        _playAccumulated += (float)delta;
        var frameTime    = 1f / Mathf.Max(1, Fps);
        while (_playAccumulated >= frameTime)
        {
            _playAccumulated -= frameTime;
            var next = _currentFrame + 1;
            if (next > TotalFrames) next = 0;
            SetCurrentFrame(next);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Store (or update) a keyframe at the given frame.</summary>
    public void SetKeyframe(int frame, Dictionary<string, Quaternion> poseData)
    {
        if (!_keyframes.TryGetValue(frame, out var existing))
        {
            existing = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
            _keyframes[frame] = existing;
        }

        foreach (var (name, rot) in poseData)
        {
            existing[name] = rot;
            if (!_trackedBones.Contains(name))
            {
                _trackedBones.Add(name);
                _trackedBones.Sort(StringComparer.OrdinalIgnoreCase);
            }
        }

        RefreshScrollRange();
        _trackArea.QueueRedraw();
    }

    /// <summary>Remove the keyframe at the given frame.</summary>
    public void RemoveKeyframe(int frame)
    {
        if (!_keyframes.Remove(frame)) return;
        RebuildTrackedBones();
        RefreshScrollRange();
        _trackArea.QueueRedraw();
    }

    /// <summary>Jump to the given frame and fire FrameChanged.</summary>
    public void SetCurrentFrame(int frame)
    {
        frame = Mathf.Clamp(frame, 0, TotalFrames);
        if (frame == _currentFrame) return;
        _currentFrame = frame;
        UpdateFrameLabel();
        _trackArea.QueueRedraw();
        FrameChanged?.Invoke(_currentFrame);
    }

    /// <summary>
    /// Return a linearly-interpolated pose at <paramref name="frame"/>.
    /// Returns null when there are no keyframes.
    /// </summary>
    public Dictionary<string, Quaternion>? GetPoseAt(int frame)
    {
        if (_keyframes.Count == 0) return null;

        if (_keyframes.TryGetValue(frame, out var exact))
            return new Dictionary<string, Quaternion>(exact, StringComparer.OrdinalIgnoreCase);

        int? prevF = null, nextF = null;
        foreach (var f in _keyframes.Keys)
        {
            if (f <= frame) prevF = f;
            else            { nextF = f; break; }
        }

        if (prevF is null)
            return new Dictionary<string, Quaternion>(_keyframes[nextF!.Value], StringComparer.OrdinalIgnoreCase);
        if (nextF is null)
            return new Dictionary<string, Quaternion>(_keyframes[prevF.Value], StringComparer.OrdinalIgnoreCase);

        var t    = (float)(frame - prevF.Value) / (nextF.Value - prevF.Value);
        var prev = _keyframes[prevF.Value];
        var next = _keyframes[nextF.Value];
        var result = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);

        foreach (var boneName in _trackedBones)
        {
            var hasPrev = prev.TryGetValue(boneName, out var rPrev);
            var hasNext = next.TryGetValue(boneName, out var rNext);
            if      (hasPrev && hasNext) result[boneName] = rPrev.Slerp(rNext, t);
            else if (hasPrev)            result[boneName] = rPrev;
            else if (hasNext)            result[boneName] = rNext;
        }

        return result;
    }

    public void Play()
    {
        if (_isPlaying) return;
        _isPlaying         = true;
        _playAccumulated   = 0f;
        _btnPlayPause.Text = "⏸";
        PlaybackStarted?.Invoke();
    }

    public void Stop()
    {
        if (!_isPlaying) return;
        _isPlaying         = false;
        _btnPlayPause.Text = "▶";
        PlaybackStopped?.Invoke();
    }

    public bool IsPlaying => _isPlaying;

    // ── Helpers ───────────────────────────────────────────────────────────

    private void OnPlayPausePressed()
    {
        if (_isPlaying) Stop();
        else Play();
    }

    private void OnStopPressed()
    {
        Stop();
        SetCurrentFrame(0);
    }

    private void UpdateFrameLabel() =>
        _frameLabel.Text = $"{_currentFrame} / {TotalFrames}";

    private void RefreshScrollRange()
    {
        var trackW  = TotalFrames * TimelineTrackArea.PixPerFrame;
        var visible = _trackArea.Size.X - TimelineTrackArea.BoneNameWidth;
        _scrollBar.MaxValue = Mathf.Max(0.0, trackW - Mathf.Max(0f, visible) + 40.0);
        _scrollBar.Page     = Mathf.Max(1.0, visible);
    }

    private void RebuildTrackedBones()
    {
        _trackedBones.Clear();
        foreach (var (_, pose) in _keyframes)
            foreach (var name in pose.Keys)
                if (!_trackedBones.Contains(name))
                    _trackedBones.Add(name);
        _trackedBones.Sort(StringComparer.OrdinalIgnoreCase);
    }
}
