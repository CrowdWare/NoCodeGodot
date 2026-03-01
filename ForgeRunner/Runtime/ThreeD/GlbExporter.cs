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
using System.Collections.Generic;
using Runtime.Logging;

namespace Runtime.ThreeD;

/// <summary>
/// Exports a posed/animated character (and optionally scene props) to a GLB file
/// using Godot's <see cref="GltfDocument"/> API.
/// </summary>
public static class GlbExporter
{
    public sealed record ExportOptions(bool IncludeAnimation, bool IncludeProps);

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Export the character (and optionally props + animation) to <paramref name="path"/>.
    /// When <see cref="ExportOptions.IncludeProps"/> is true the model is temporarily
    /// re-parented into a throw-away scene root; the caller must restore it afterwards.
    /// </summary>
    /// <param name="restoreParent">
    /// Out: the node to which <paramref name="modelRoot"/> should be re-added after this call
    /// returns (only non-null when IncludeProps=true and modelRoot was detached).
    /// </param>
    public static void Export(
        Node3D     modelRoot,
        Skeleton3D skeleton,
        SortedDictionary<int, Dictionary<string, Quaternion>> keyframes,
        int        fps,
        int        totalFrames,
        IEnumerable<(Node3D Node, string Name)> props,
        ExportOptions options,
        string     path,
        out Node?  restoreParent)
    {
        restoreParent = null;

        // ── 0. Capture skeleton path BEFORE any re-parenting ──────────────────
        // GetPathTo requires both nodes to share a common scene-tree ancestor.
        var skelPath = modelRoot.GetPathTo(skeleton);

        Node3D exportRoot = modelRoot;
        Node3D? tempRoot  = null;

        // ── 1. Build export root (wrap model + props if needed) ───────────────
        if (options.IncludeProps)
        {
            restoreParent = modelRoot.GetParent();
            restoreParent?.RemoveChild(modelRoot);

            tempRoot = new Node3D { Name = "Scene" };
            tempRoot.AddChild(modelRoot);
            foreach (var (node, name) in props)
            {
                var dup  = (Node3D)node.Duplicate();
                dup.Name = name;
                tempRoot.AddChild(dup);
            }
            exportRoot = tempRoot;
        }

        // ── 2. Bake animation tracks (if requested) ───────────────────────────
        AnimationPlayer? animPlayer = null;
        if (options.IncludeAnimation && keyframes.Count > 0)
        {
            var normToOrig = BuildNormToOrigMap(skeleton);
            var anim       = new Animation
            {
                Length   = (float)totalFrames / fps,
                LoopMode = Animation.LoopModeEnum.Linear,
            };

            var boneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var frame in keyframes.Values)
                foreach (var k in frame.Keys) boneNames.Add(k);

            foreach (var normBone in boneNames)
            {
                if (!normToOrig.TryGetValue(normBone, out var origBone)) continue;
                var idx = anim.AddTrack(Animation.TrackType.Rotation3D);
                anim.TrackSetPath(idx, $"{skelPath}:{origBone}");
                anim.TrackSetInterpolationType(idx, Animation.InterpolationType.Linear);
                foreach (var (frame, pose) in keyframes)
                {
                    if (!pose.TryGetValue(normBone, out var rot)) continue;
                    anim.TrackInsertKey(idx, (float)frame / fps, rot);
                }
            }

            var lib = new AnimationLibrary();
            lib.AddAnimation("animation", anim);

            animPlayer = new AnimationPlayer { Name = "AnimationPlayer" };
            // Always attach to modelRoot so RootNode=".." always resolves to modelRoot,
            // regardless of whether IncludeProps wraps everything in a tempRoot.
            modelRoot.AddChild(animPlayer);
            animPlayer.RootNode = new NodePath("..");
            animPlayer.AddAnimationLibrary("", lib);
        }

        // ── 3. Write GLB ──────────────────────────────────────────────────────
        WriteGlb(exportRoot, path);

        // ── 4. Cleanup (remove what we added) ────────────────────────────────
        if (animPlayer is not null)
        {
            modelRoot.RemoveChild(animPlayer);
            animPlayer.QueueFree();
        }
        if (tempRoot is not null)
        {
            // Detach modelRoot so the caller can re-add it to _worldRoot
            tempRoot.RemoveChild(modelRoot);
            tempRoot.QueueFree();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void WriteGlb(Node root, string path)
    {
        var state = new GltfState();
        var doc   = new GltfDocument();

        var err = doc.AppendFromScene(root, state);
        if (err != Error.Ok)
        {
            RunnerLogger.Error("GlbExporter", $"AppendFromScene failed: {err}");
            return;
        }
        err = doc.WriteToFilesystem(state, path);
        if (err != Error.Ok)
            RunnerLogger.Error("GlbExporter", $"WriteToFilesystem failed: {err}");
        else
            RunnerLogger.Info("GlbExporter", $"Exported GLB: {path}");
    }

    private static Dictionary<string, string> BuildNormToOrigMap(Skeleton3D skeleton)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < skeleton.GetBoneCount(); i++)
        {
            var orig = skeleton.GetBoneName(i);
            map.TryAdd(BoneNameNormalizer.Normalize(orig), orig);
        }
        return map;
    }
}
