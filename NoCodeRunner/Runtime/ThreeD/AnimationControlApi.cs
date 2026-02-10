using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;

namespace Runtime.ThreeD;

public sealed class AnimationControlApi
{
    private readonly Dictionary<string, AnimationPlayer> _playersById = new(StringComparer.OrdinalIgnoreCase);

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

        player.Stop();
        RunnerLogger.Info("3D", $"Stopped animations for id '{id}'.");
        return true;
    }
}
