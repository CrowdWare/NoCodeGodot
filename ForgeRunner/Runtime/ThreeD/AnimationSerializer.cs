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
using Runtime.Sml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Runtime.ThreeD;

// ── Data model ───────────────────────────────────────────────────────────────

public sealed record SceneAssetData(
    string Id,
    string Src,
    bool   Primary,
    float  PosX,
    float  PosY,
    float  PosZ,
    float  RotY,
    float  Scale);

public sealed record AnimationProjectData(
    int                                                      Fps,
    int                                                      TotalFrames,
    List<SceneAssetData>                                     SceneAssets,
    SortedDictionary<int, Dictionary<string, Quaternion>>    Keyframes);

// ── Serializer ───────────────────────────────────────────────────────────────

/// <summary>
/// Reads and writes <c>.fpose</c> files (SML syntax).
///
/// Format overview:
/// <code>
/// PoseProject {
///     fps: 24
///     totalFrames: 120
///
///     Scene {
///         Asset { id: "char"; src: "/path/model.glb"; primary: true; posX: 0; posY: 0; posZ: 0; rotY: 0; scale: 1 }
///     }
///
///     Animation {
///         Keyframe { frame: 0
///             Bone { name: "Hips"; x: 0; y: 0; z: 0; w: 1 }
///         }
///     }
/// }
/// </code>
/// Bones use quaternion components (x, y, z, w) to avoid Euler conversion
/// precision loss.  Bone names are stored already normalized.
/// </summary>
public static class AnimationSerializer
{
    // ── Serialize ─────────────────────────────────────────────────────────

    public static string Serialize(AnimationProjectData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PoseProject {");
        sb.AppendLine($"    fps: {data.Fps}");
        sb.AppendLine($"    totalFrames: {data.TotalFrames}");
        sb.AppendLine();

        // Scene
        sb.AppendLine("    Scene {");
        foreach (var asset in data.SceneAssets)
        {
            sb.Append("        Asset {");
            sb.Append($" id: \"{Esc(asset.Id)}\"");
            sb.Append($" src: \"{Esc(asset.Src)}\"");
            if (asset.Primary) sb.Append(" primary: true");
            sb.Append($" posX: {F(asset.PosX)} posY: {F(asset.PosY)} posZ: {F(asset.PosZ)}");
            sb.Append($" rotY: {F(asset.RotY)} scale: {F(asset.Scale)}");
            sb.AppendLine(" }");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // Animation
        sb.AppendLine("    Animation {");
        foreach (var (frame, bones) in data.Keyframes)
        {
            sb.AppendLine($"        Keyframe {{ frame: {frame}");
            foreach (var (boneName, q) in bones)
            {
                sb.Append($"            Bone {{");
                sb.Append($" name: \"{Esc(boneName)}\"");
                sb.Append($" x: {F(q.X)} y: {F(q.Y)} z: {F(q.Z)} w: {F(q.W)}");
                sb.AppendLine(" }");
            }
            sb.AppendLine("        }");
        }
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ── Deserialize ───────────────────────────────────────────────────────

    public static AnimationProjectData? Deserialize(string sml)
    {
        SmlDocument doc;
        try
        {
            doc = new SmlParser(sml).ParseDocument();
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("AnimationSerializer", "Failed to parse .fpose file.", ex);
            return null;
        }

        SmlNode? root = null;
        foreach (var node in doc.Roots)
        {
            if (string.Equals(node.Name, "PoseProject", StringComparison.OrdinalIgnoreCase))
            {
                root = node;
                break;
            }
        }

        if (root is null)
        {
            RunnerLogger.Warn("AnimationSerializer", "No PoseProject root node found.");
            return null;
        }

        var fps         = GetInt(root, "fps",         24);
        var totalFrames = GetInt(root, "totalFrames", 120);

        var assets    = new List<SceneAssetData>();
        var keyframes = new SortedDictionary<int, Dictionary<string, Quaternion>>();

        foreach (var child in root.Children)
        {
            if (string.Equals(child.Name, "Scene", StringComparison.OrdinalIgnoreCase))
                ParseScene(child, assets);
            else if (string.Equals(child.Name, "Animation", StringComparison.OrdinalIgnoreCase))
                ParseAnimation(child, keyframes);
        }

        return new AnimationProjectData(fps, totalFrames, assets, keyframes);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void ParseScene(SmlNode scene, List<SceneAssetData> assets)
    {
        foreach (var node in scene.Children)
        {
            if (!string.Equals(node.Name, "Asset", StringComparison.OrdinalIgnoreCase)) continue;

            var id      = GetString(node, "id",      $"asset{assets.Count}");
            var src     = GetString(node, "src",     string.Empty);
            var primary = GetBool  (node, "primary", false);
            var posX    = GetFloat (node, "posX",    0f);
            var posY    = GetFloat (node, "posY",    0f);
            var posZ    = GetFloat (node, "posZ",    0f);
            var rotY    = GetFloat (node, "rotY",    0f);
            var scale   = GetFloat (node, "scale",   1f);

            assets.Add(new SceneAssetData(id, src, primary, posX, posY, posZ, rotY, scale));
        }
    }

    private static void ParseAnimation(SmlNode anim, SortedDictionary<int, Dictionary<string, Quaternion>> keyframes)
    {
        foreach (var kfNode in anim.Children)
        {
            if (!string.Equals(kfNode.Name, "Keyframe", StringComparison.OrdinalIgnoreCase)) continue;

            var frame = GetInt(kfNode, "frame", -1);
            if (frame < 0) continue;

            var bones = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);

            foreach (var boneNode in kfNode.Children)
            {
                if (!string.Equals(boneNode.Name, "Bone", StringComparison.OrdinalIgnoreCase)) continue;

                var name = GetString(boneNode, "name", string.Empty);
                if (string.IsNullOrWhiteSpace(name)) continue;

                var x = GetFloat(boneNode, "x", 0f);
                var y = GetFloat(boneNode, "y", 0f);
                var z = GetFloat(boneNode, "z", 0f);
                var w = GetFloat(boneNode, "w", 1f);
                bones[name] = new Quaternion(x, y, z, w).Normalized();
            }

            if (bones.Count > 0)
                keyframes[frame] = bones;
        }
    }

    // ── SmlNode value helpers ─────────────────────────────────────────────

    private static string GetString(SmlNode node, string key, string fallback)
    {
        if (!node.TryGetProperty(key, out var v)) return fallback;
        return v.Kind is SmlValueKind.String or SmlValueKind.Identifier
            ? (string)v.Value
            : fallback;
    }

    private static int GetInt(SmlNode node, string key, int fallback)
    {
        if (!node.TryGetProperty(key, out var v)) return fallback;
        return v.Kind switch
        {
            SmlValueKind.Int   => (int)v.Value,
            SmlValueKind.Float => (int)(double)v.Value,
            _                  => fallback
        };
    }

    private static float GetFloat(SmlNode node, string key, float fallback)
    {
        if (!node.TryGetProperty(key, out var v)) return fallback;
        return v.Kind switch
        {
            SmlValueKind.Float => (float)(double)v.Value,
            SmlValueKind.Int   => (float)(int)v.Value,
            _                  => fallback
        };
    }

    private static bool GetBool(SmlNode node, string key, bool fallback)
    {
        if (!node.TryGetProperty(key, out var v)) return fallback;
        return v.Kind == SmlValueKind.Bool ? (bool)v.Value : fallback;
    }

    private static string F(float v) =>
        v.ToString("G6", CultureInfo.InvariantCulture);

    private static string Esc(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
