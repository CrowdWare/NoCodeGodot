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

    // ── Two-phase context ─────────────────────────────────────────────────────

    /// <summary>
    /// Intermediate state returned by <see cref="Prepare"/> and consumed by
    /// <see cref="Write"/>.  modelRoot is detached from the scene tree for the
    /// lifetime of this context.
    /// </summary>
    public sealed class GltfWriteContext
    {
        internal GltfState        State      { get; init; } = null!;
        internal GltfDocument     Doc        { get; init; } = null!;
        internal Node3D           ModelRoot  { get; init; } = null!;
        internal AnimationPlayer? AnimPlayer { get; init; }
        internal Node3D?          TempRoot   { get; init; }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 1: detach modelRoot, bake animation, call AppendFromScene.
    /// Returns null on failure; modelRoot is re-attached to its original parent
    /// in that case.
    /// </summary>
    public static GltfWriteContext? Prepare(
        Node3D     modelRoot,
        Skeleton3D skeleton,
        SortedDictionary<int, Dictionary<string, Quaternion>> keyframes,
        int        fps,
        int        totalFrames,
        IEnumerable<(Node3D Node, string Name)> props,
        ExportOptions options)
    {
        // ── 0. Capture skeleton path BEFORE any re-parenting ──────────────────
        var skelPath = modelRoot.GetPathTo(skeleton);

        // ── 1. Detach modelRoot so AppendFromScene sees only its subtree ───────
        var originalParent = modelRoot.GetParent();
        originalParent?.RemoveChild(modelRoot);

        Node3D exportRoot = modelRoot;
        Node3D? tempRoot  = null;

        // ── 2. Wrap model + props when requested ──────────────────────────────
        if (options.IncludeProps)
        {
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

        // ── 3. Bake animation tracks ──────────────────────────────────────────
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
            modelRoot.AddChild(animPlayer);
            animPlayer.RootNode = new NodePath("..");
            animPlayer.AddAnimationLibrary("", lib);
        }

        // ── 4. Build GLTF state ───────────────────────────────────────────────
        var state = new GltfState();
        var doc   = new GltfDocument();
        var err   = doc.AppendFromScene(exportRoot, state);
        if (err != Error.Ok)
        {
            RunnerLogger.Error("GlbExporter", $"AppendFromScene failed: {err}");
            Cleanup(modelRoot, animPlayer, tempRoot);
            originalParent?.AddChild(modelRoot);   // restore on failure
            return null;
        }

        return new GltfWriteContext
        {
            State     = state,
            Doc       = doc,
            ModelRoot = modelRoot,
            AnimPlayer = animPlayer,
            TempRoot  = tempRoot,
        };
    }

    /// <summary>
    /// Phase 2: write GLTF state to disk and clean up temporary nodes.
    /// After this call modelRoot has no parent — the caller must re-attach it.
    /// </summary>
    /// <returns>true on success.</returns>
    public static bool Write(GltfWriteContext ctx, string path)
    {
        var err = ctx.Doc.WriteToFilesystem(ctx.State, path);
        Cleanup(ctx.ModelRoot, ctx.AnimPlayer, ctx.TempRoot);

        if (err != Error.Ok)
        {
            RunnerLogger.Error("GlbExporter", $"WriteToFilesystem failed: {err}");
            return false;
        }

        RunnerLogger.Info("GlbExporter", $"Exported GLB: {path}");
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Cleanup(Node3D modelRoot, AnimationPlayer? animPlayer, Node3D? tempRoot)
    {
        if (animPlayer is not null)
        {
            modelRoot.RemoveChild(animPlayer);
            animPlayer.QueueFree();
        }
        if (tempRoot is not null)
        {
            tempRoot.RemoveChild(modelRoot);
            tempRoot.QueueFree();
        }
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
