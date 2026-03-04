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
using System.Reflection;
using Runtime.Logging;

namespace Runtime.ThreeD;

/// <summary>
/// Exports a posed/animated character (and optionally scene props) to a GLB file
/// using Godot's <see cref="GltfDocument"/> API.
/// </summary>
public static class GlbExporter
{
    public sealed record ExportOptions(
        bool IncludeAnimation,
        bool IncludeProps,
        bool AnimationOnlyCharacter = false,
        bool WithRig = true);

    // ── Two-phase context ─────────────────────────────────────────────────────

    /// <summary>
    /// Intermediate state returned by <see cref="Prepare"/> and consumed by
    /// <see cref="Write"/>.  modelRoot is detached from the scene tree for the
    /// lifetime of this context.
    /// </summary>
    public sealed class GltfWriteContext
    {
        internal GltfState    State      { get; init; } = null!;
        internal GltfDocument Doc        { get; init; } = null!;
        internal Node3D       RootToFree { get; init; } = null!;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 1: duplicate and sanitize export subtree, optionally bake animation,
    /// then call AppendFromScene.
    /// Returns null on failure.
    /// </summary>
    public static GltfWriteContext? Prepare(
        Node3D     modelRoot,
        Dictionary<string, Quaternion> currentPose,
        SortedDictionary<int, Dictionary<string, Quaternion>> keyframes,
        int        fps,
        int        totalFrames,
        IEnumerable<(Node3D Node, string Name)> props,
        ExportOptions options,
        Action<int, string>? progress = null)
    {
        progress?.Invoke(5, "Clone model");
        var exportCharacter = CloneNode3D(modelRoot);
        if (exportCharacter is null)
        {
            RunnerLogger.Error("GlbExporter", "Failed to clone model root for export.");
            return null;
        }

        progress?.Invoke(12, "Sanitize model");
        SanitizeForExport(exportCharacter, removeCharacterMeshes: options.AnimationOnlyCharacter);

        var exportSkeleton = FindSkeleton(exportCharacter);
        if (exportSkeleton is null)
        {
            RunnerLogger.Error("GlbExporter", "No Skeleton3D found in cloned export tree.");
            exportCharacter.QueueFree();
            return null;
        }

        // ── Bake animation tracks ────────────────────────────────────────────
        if (options.IncludeAnimation && keyframes.Count > 0)
        {
            progress?.Invoke(26, "Build animation tracks");
            var skelPath   = exportCharacter.GetPathTo(exportSkeleton);
            var normToOrig = BuildNormToOrigMap(exportSkeleton);
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

            var animPlayer = new AnimationPlayer { Name = "AnimationPlayer" };
            exportCharacter.AddChild(animPlayer);
            animPlayer.RootNode = new NodePath("..");
            animPlayer.AddAnimationLibrary("", lib);
        }
        else if (!options.WithRig)
        {
            progress?.Invoke(26, "Apply static pose");
            // Workaround for Godot GLTF skin/bind roundtrip issues in DCC tools:
            // for static exports, detach meshes from skeleton and remove armature nodes.
            ApplyStaticPose(exportSkeleton, currentPose, keyframes);
            AttachForBakeIfPossible(modelRoot, exportCharacter);
            progress?.Invoke(36, "Bake static mesh");
            var staticOk = ConvertCharacterToStaticMesh(exportCharacter);
            DetachIfParented(exportCharacter);
            if (!staticOk)
            {
                RunnerLogger.Warn("GlbExporter", "No-Rig export requested, but static bake failed for this asset. Keeping rig to avoid tiny/invalid mesh.");
            }
        }

        Node3D exportRoot = exportCharacter;
        Node3D rootToFree = exportCharacter;

        if (options.IncludeProps)
        {
            progress?.Invoke(45, "Clone scene props");
            var sceneRoot = new Node3D { Name = "Scene" };
            sceneRoot.AddChild(exportCharacter);
            foreach (var (node, name) in props)
            {
                var dup = CloneNode3D(node);
                if (dup is null)
                {
                    continue;
                }

                dup.Name = name;
                SanitizeForExport(dup, removeCharacterMeshes: false);
                sceneRoot.AddChild(dup);
            }

            exportRoot = sceneRoot;
            rootToFree = sceneRoot;
        }

        // ── Build GLTF state ─────────────────────────────────────────────────
        progress?.Invoke(62, "Build GLTF scene");
        var state = new GltfState();
        var doc   = new GltfDocument();
        var err   = doc.AppendFromScene(exportRoot, state);
        if (err != Error.Ok)
        {
            RunnerLogger.Error("GlbExporter", $"AppendFromScene failed: {err}");
            rootToFree.QueueFree();
            return null;
        }

        return new GltfWriteContext
        {
            State      = state,
            Doc        = doc,
            RootToFree = rootToFree,
        };
    }

    /// <summary>
    /// Phase 2: write GLTF state to disk and clean up temporary nodes.
    /// After this call modelRoot has no parent — the caller must re-attach it.
    /// </summary>
    /// <returns>true on success.</returns>
    public static bool Write(GltfWriteContext ctx, string path, Action<int, string>? progress = null)
    {
        progress?.Invoke(82, "Write GLB file");
        var err = ctx.Doc.WriteToFilesystem(ctx.State, path);
        ctx.RootToFree.QueueFree();

        if (err != Error.Ok)
        {
            RunnerLogger.Error("GlbExporter", $"WriteToFilesystem failed: {err}");
            return false;
        }

        progress?.Invoke(98, "Finalize export");
        RunnerLogger.Info("GlbExporter", $"Exported GLB: {path}");
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Node3D? CloneNode3D(Node3D source)
    {
        return source.Duplicate() as Node3D;
    }

    private static void SanitizeForExport(Node root, bool removeCharacterMeshes)
    {
        var toRemove = new List<Node>();
        CollectNodesToRemove(root, removeCharacterMeshes, toRemove);
        foreach (var node in toRemove)
        {
            var parent = node.GetParent();
            parent?.RemoveChild(node);
            node.QueueFree();
        }
    }

    private static void CollectNodesToRemove(Node node, bool removeCharacterMeshes, List<Node> output)
    {
        foreach (Node child in node.GetChildren())
        {
            CollectNodesToRemove(child, removeCharacterMeshes, output);
        }

        if (ShouldRemoveNode(node, removeCharacterMeshes))
        {
            output.Add(node);
        }
    }

    private static bool ShouldRemoveNode(Node node, bool removeCharacterMeshes)
    {
        var typeName = node.GetType().Name;
        var nodeName = node.Name.ToString();

        if (node is AnimationPlayer)
            return true;

        if (nodeName.Equals("GLTF_not_exported", StringComparison.OrdinalIgnoreCase))
            return true;

        if (HasBlockedAncestor(node))
            return true;

        if (typeName.Equals("PhysicalBoneSimulator3D", StringComparison.Ordinal)
            || nodeName.StartsWith("_PhysicalBoneSimulator3D", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (node is PhysicalBone3D
            || node is CollisionShape3D
            || node is CollisionPolygon3D)
        {
            return true;
        }

        if (typeName.Contains("PhysicalBone", StringComparison.Ordinal)
            || typeName.Contains("CollisionShape", StringComparison.Ordinal))
        {
            return true;
        }

        if (node is MeshInstance3D
            && (nodeName.Contains("icosphere", StringComparison.OrdinalIgnoreCase)
                || nodeName.Contains("collision", StringComparison.OrdinalIgnoreCase)
                || nodeName.Contains("physics", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (removeCharacterMeshes
            && (node is MeshInstance3D || typeName.Contains("MeshInstance3D", StringComparison.Ordinal)))
        {
            return true;
        }

        return false;
    }

    private static bool HasBlockedAncestor(Node node)
    {
        var p = node.GetParent();
        while (p is not null)
        {
            var parentName = p.Name.ToString();
            var parentType = p.GetType().Name;
            if (parentName.Equals("GLTF_not_exported", StringComparison.OrdinalIgnoreCase)
                || parentName.StartsWith("_PhysicalBoneSimulator3D", StringComparison.OrdinalIgnoreCase)
                || parentType.Contains("PhysicalBone", StringComparison.Ordinal)
                || parentType.Contains("CollisionShape", StringComparison.Ordinal))
            {
                return true;
            }

            p = p.GetParent();
        }

        return false;
    }

    private static Skeleton3D? FindSkeleton(Node node)
    {
        if (node is Skeleton3D skeleton)
            return skeleton;

        foreach (Node child in node.GetChildren())
        {
            var found = FindSkeleton(child);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static bool ConvertCharacterToStaticMesh(Node3D exportCharacter)
    {
        var skel = FindSkeleton(exportCharacter);
        skel?.ForceUpdateAllBoneTransforms();
        exportCharacter.ForceUpdateTransform();

        var meshes = new List<MeshInstance3D>();
        CollectMeshes(exportCharacter, meshes);

        var bakedMeshes = new Dictionary<MeshInstance3D, Mesh>();
        var bakedCount = 0;
        foreach (var mesh in meshes)
        {
            if (TryBakeMeshFromCurrentSkeletonPose(mesh, out var bakedMesh))
            {
                bakedCount++;
                bakedMeshes[mesh] = bakedMesh;
            }
        }

        if (meshes.Count > 0 && bakedCount == 0)
        {
            RunnerLogger.Warn("GlbExporter", "Static mesh bake produced no baked meshes.");
            return false;
        }
        else if (bakedCount < meshes.Count)
        {
            RunnerLogger.Warn("GlbExporter", $"Static mesh bake was partial ({bakedCount}/{meshes.Count}).");
            return false;
        }
        else
        {
            RunnerLogger.Info("GlbExporter", $"Static mesh bake succeeded ({bakedCount}/{meshes.Count}); removing rig/armature.");
        }

        foreach (var (mesh, bakedMesh) in bakedMeshes)
        {
            mesh.Mesh = bakedMesh;
            TryClearSkeletonBinding(mesh);
        }

        RemoveArmatureNodes(exportCharacter);
        return true;
    }

    private static void ApplyStaticPose(
        Skeleton3D skeleton,
        Dictionary<string, Quaternion> currentPose,
        SortedDictionary<int, Dictionary<string, Quaternion>> keyframes)
    {
        Dictionary<string, Quaternion>? pose = null;
        if (currentPose.Count > 0)
        {
            pose = currentPose;
        }
        else if (keyframes.TryGetValue(0, out pose))
        {
            // use frame 0 when available
        }
        else if (keyframes.Count > 0)
        {
            pose = keyframes.First().Value;
        }

        if (pose is null || pose.Count == 0)
            return;

        var normToOrig = BuildNormToOrigMap(skeleton);
        foreach (var (boneName, rotation) in pose)
        {
            var resolvedName = boneName;
            if (!normToOrig.TryGetValue(boneName, out resolvedName))
            {
                var normalized = BoneNameNormalizer.Normalize(boneName);
                if (!normToOrig.TryGetValue(normalized, out resolvedName))
                    continue;
            }

            var boneIndex = skeleton.FindBone(resolvedName);
            if (boneIndex < 0)
                continue;

            skeleton.SetBonePoseRotation(boneIndex, rotation);
        }

        skeleton.ForceUpdateAllBoneTransforms();
    }

    private static void CollectMeshes(Node node, List<MeshInstance3D> output)
    {
        if (node is MeshInstance3D mesh)
            output.Add(mesh);

        foreach (Node child in node.GetChildren())
            CollectMeshes(child, output);
    }

    private static bool TryBakeMeshFromCurrentSkeletonPose(MeshInstance3D mesh, out Mesh bakedMesh)
    {
        bakedMesh = null!;
        try
        {
            var originalMesh = mesh.Mesh;
            var method = mesh.GetType().GetMethod("BakeMeshFromCurrentSkeletonPose",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (method is null)
                return false;

            var baked = method.Invoke(mesh, null);
            if (baked is Mesh candidate)
            {
                if (!LooksLikeValidBake(originalMesh, candidate))
                {
                    RunnerLogger.Warn("GlbExporter", $"Rejecting baked mesh for '{mesh.Name}' due to invalid scale/geometry.");
                    return false;
                }
                bakedMesh = candidate;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("GlbExporter", $"BakeMeshFromCurrentSkeletonPose failed for '{mesh.Name}': {ex.Message}");
            return false;
        }
    }

    private static bool LooksLikeValidBake(Mesh? original, Mesh baked)
    {
        if (baked.GetSurfaceCount() <= 0)
            return false;

        if (original is null)
            return true;

        var origAabb = original.GetAabb();
        var bakedAabb = baked.GetAabb();
        var origLen = origAabb.Size.Length();
        var bakedLen = bakedAabb.Size.Length();
        if (origLen <= 0.0001f || bakedLen <= 0.0001f)
            return false;

        // Keep validation permissive. Rigged assets can legitimately differ a lot in local AABB
        // after baking because of import/unit scale and bind-pose space.
        return true;
    }

    private static void TryClearSkeletonBinding(MeshInstance3D mesh)
    {
        TrySetProperty(mesh, "Skeleton", new NodePath());
        TrySetProperty(mesh, "SkeletonPath", new NodePath());
        TrySetProperty(mesh, "Skin", null);
    }

    private static void TrySetProperty(object instance, string name, object? value)
    {
        try
        {
            var prop = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite)
                return;
            prop.SetValue(instance, value);
        }
        catch
        {
            // best effort for API differences between Godot versions
        }
    }

    private static void RemoveArmatureNodes(Node root)
    {
        var toRemove = new List<Node>();
        CollectArmatureNodes(root, toRemove);
        foreach (var node in toRemove)
        {
            var parent = node.GetParent();
            if (parent is null)
                continue;

            var children = new List<Node>();
            foreach (Node child in node.GetChildren())
                children.Add(child);

            foreach (var child in children)
            {
                var oldParentLocal = node is Node3D parentNode3 ? parentNode3.Transform : Transform3D.Identity;
                node.RemoveChild(child);
                parent.AddChild(child);
                if (child is Node3D child3)
                {
                    child3.Transform = oldParentLocal * child3.Transform;
                }
            }

            parent.RemoveChild(node);
            node.QueueFree();
        }
    }

    private static void CollectArmatureNodes(Node node, List<Node> output)
    {
        foreach (Node child in node.GetChildren())
            CollectArmatureNodes(child, output);

        if (node is Skeleton3D
            || node.Name.ToString().Equals("Armature", StringComparison.OrdinalIgnoreCase))
        {
            output.Add(node);
        }
    }

    private static void ForceStripRigNodes(Node root)
    {
        var toRemove = new List<Node>();
        CollectRigNodes(root, toRemove);
        foreach (var node in toRemove)
        {
            var parent = node.GetParent();
            if (parent is null)
                continue;

            var children = new List<Node>();
            foreach (Node child in node.GetChildren())
                children.Add(child);

            foreach (var child in children)
            {
                node.RemoveChild(child);
                parent.AddChild(child);
            }

            parent.RemoveChild(node);
            node.QueueFree();
        }
    }

    private static void CollectRigNodes(Node node, List<Node> output)
    {
        foreach (Node child in node.GetChildren())
            CollectRigNodes(child, output);

        var typeName = node.GetType().Name;
        var nodeName = node.Name.ToString();
        if (node is Skeleton3D
            || node is AnimationPlayer
            || typeName.Contains("BoneAttachment", StringComparison.Ordinal)
            || typeName.Contains("PhysicalBone", StringComparison.Ordinal)
            || nodeName.Equals("Armature", StringComparison.OrdinalIgnoreCase)
            || nodeName.StartsWith("Armature", StringComparison.OrdinalIgnoreCase))
        {
            output.Add(node);
        }
    }

    private static void AttachForBakeIfPossible(Node3D modelRoot, Node3D exportCharacter)
    {
        if (exportCharacter.GetParent() is not null)
            return;

        var parent = modelRoot.GetParent();
        if (parent is null)
            return;

        parent.AddChild(exportCharacter);
    }

    private static void DetachIfParented(Node3D node)
    {
        var parent = node.GetParent();
        parent?.RemoveChild(node);
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
