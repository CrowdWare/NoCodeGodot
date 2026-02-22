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

namespace Runtime.ThreeD;

public sealed class AnimationControlApi
{
    private readonly Dictionary<string, AnimationPlayer> _playersById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _lastAnimationById = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string id, AnimationPlayer player)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _playersById[id] = player;
        RunnerLogger.Info("3D", $"AnimationPlayer registered for id '{id}'.");
    }

    public IReadOnlyList<string> ListAnimations(string id)
    {
        if (!_playersById.TryGetValue(id, out var player))
        {
            RunnerLogger.Warn("3D", $"No AnimationPlayer registered for id '{id}'.");
            return Array.Empty<string>();
        }

        var list = new List<string>();
        foreach (string animation in player.GetAnimationList())
        {
            list.Add(animation);
        }

        return list;
    }

    public bool Play(string id, string animationName)
    {
        if (!_playersById.TryGetValue(id, out var player))
        {
            RunnerLogger.Warn("3D", $"No AnimationPlayer registered for id '{id}'.");
            return false;
        }

        if (!player.HasAnimation(animationName))
        {
            RunnerLogger.Warn("3D", $"Animation '{animationName}' not found for id '{id}'.");
            return false;
        }

        player.Play(animationName);
        _lastAnimationById[id] = animationName;
        RunnerLogger.Info("3D", $"Playing animation '{animationName}' for id '{id}'.");
        return true;
    }

    public bool Stop(string id)
    {
        if (!_playersById.TryGetValue(id, out var player))
        {
            RunnerLogger.Warn("3D", $"No AnimationPlayer registered for id '{id}'.");
            return false;
        }

        var current = player.CurrentAnimation.ToString();
        if (!string.IsNullOrWhiteSpace(current) && player.HasAnimation(current))
        {
            _lastAnimationById[id] = current;
        }

        player.Stop();
        RunnerLogger.Info("3D", $"Stopped animations for id '{id}'.");
        return true;
    }

    public bool Rewind(string id)
    {
        if (!_playersById.TryGetValue(id, out var player))
        {
            RunnerLogger.Warn("3D", $"No AnimationPlayer registered for id '{id}'.");
            return false;
        }

        var current = player.CurrentAnimation.ToString();
        if (string.IsNullOrWhiteSpace(current) || !player.HasAnimation(current))
        {
            if (!_lastAnimationById.TryGetValue(id, out current)
                || string.IsNullOrWhiteSpace(current)
                || !player.HasAnimation(current))
            {
                RunnerLogger.Warn("3D", $"No current animation to rewind for id '{id}'.");
                return false;
            }

            player.Play(current);
        }

        _lastAnimationById[id] = current;
        player.Seek(0.0, true);
        player.Pause();
        RunnerLogger.Info("3D", $"Rewound animation '{current}' for id '{id}'.");
        return true;
    }

    public bool SeekNormalized(string id, float normalized)
    {
        if (!_playersById.TryGetValue(id, out var player))
        {
            RunnerLogger.Warn("3D", $"No AnimationPlayer registered for id '{id}'.");
            return false;
        }

        var current = player.CurrentAnimation.ToString();
        if (string.IsNullOrWhiteSpace(current) || !player.HasAnimation(current))
        {
            if (!_lastAnimationById.TryGetValue(id, out current)
                || string.IsNullOrWhiteSpace(current)
                || !player.HasAnimation(current))
            {
                RunnerLogger.Warn("3D", $"No current animation to seek for id '{id}'.");
                return false;
            }

            // Ensure a concrete animation track is active so scrub updates the visible pose.
            player.Play(current);
        }

        var animation = player.GetAnimation(current);
        if (animation is null || animation.Length <= 0.0)
        {
            RunnerLogger.Warn("3D", $"Current animation '{current}' has invalid length for id '{id}'.");
            return false;
        }

        var clamped = Mathf.Clamp(normalized, 0f, 1f);
        var time = animation.Length * clamped;
        player.Seek(time, true);
        player.Pause();
        _lastAnimationById[id] = current;
        RunnerLogger.Info("3D", $"Seeked animation '{current}' for id '{id}' to {clamped:0.###} ({time:0.###}s).");
        return true;
    }
}
