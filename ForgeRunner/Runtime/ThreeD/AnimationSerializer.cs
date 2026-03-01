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
    string Name,
    string Src,
    bool   Primary,
    float  PosX,
    float  PosY,
    float  PosZ,
    float  RotX,
    float  RotY,
    float  RotZ,
    float  ScaleX,
    float  ScaleY,
    float  ScaleZ);

public sealed record AnimationProjectData(
    int                                                      Fps,
    int                                                      TotalFrames,
    List<SceneAssetData>                                     SceneAssets,
    SortedDictionary<int, Dictionary<string, Quaternion>>    Keyframes);

// ── Serializer ───────────────────────────────────────────────────────────────

/// <summary>
/// Reads and writes <c>.scene</c> files (SML syntax).
///
/// .scene format:
/// <code>
/// Scene {
///     Character {
///         id: "hero"  name: "hero"  src: "res://assets/models/hero.glb"
///         pos: 0.0, 0.0, 0.0
///         rot: 0.0, 0.0, 0.0
///         scale: 1.0, 1.0, 1.0
///
///         Animation {
///             fps: 24
///             totalFrames: 120
///             Key { frame: 0
///                 Bone { name: "Hips"  x: 0  y: 0  z: 0  w: 1 }
///             }
///         }
///     }
///
///     Asset {
///         id: "wall"  name: "wall"  src: "res://assets/models/wall.glb"
///         pos: 2.0, 0.0, 0.0
///         rot: 0.0, 45.0, 0.0
///         scale: 1.0, 1.0, 1.0
///     }
/// }
/// </code>
/// </summary>
public static class AnimationSerializer
{
    // ── Serialize (.scene) ────────────────────────────────────────────────

    public static string Serialize(AnimationProjectData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Scene {");

        // Character (primary asset) with embedded Animation block
        var character = data.SceneAssets.Find(a => a.Primary);
        var charId    = character?.Id   ?? "hero";
        var charName  = character?.Name ?? "hero";
        var charSrc   = character?.Src  ?? string.Empty;

        sb.AppendLine($"    Character {{");
        sb.AppendLine($"        id: \"{Esc(charId)}\"  name: \"{Esc(charName)}\"  src: \"{Esc(charSrc)}\"");
        if (character is not null)
        {
            sb.AppendLine($"        pos: {F(character.PosX)}, {F(character.PosY)}, {F(character.PosZ)}");
            sb.AppendLine($"        rot: {F(character.RotX)}, {F(character.RotY)}, {F(character.RotZ)}");
            sb.AppendLine($"        scale: {F(character.ScaleX)}, {F(character.ScaleY)}, {F(character.ScaleZ)}");
        }
        sb.AppendLine();

        // Embedded Animation block
        sb.AppendLine($"        Animation {{");
        sb.AppendLine($"            fps: {data.Fps}");
        sb.AppendLine($"            totalFrames: {data.TotalFrames}");
        foreach (var (frame, bones) in data.Keyframes)
        {
            sb.AppendLine($"            Key {{ frame: {frame}");
            foreach (var (boneName, q) in bones)
            {
                sb.Append($"                Bone {{");
                sb.Append($" name: \"{Esc(boneName)}\"");
                sb.Append($" x: {F(q.X)} y: {F(q.Y)} z: {F(q.Z)} w: {F(q.W)}");
                sb.AppendLine(" }");
            }
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        // Props (non-primary assets)
        foreach (var asset in data.SceneAssets)
        {
            if (asset.Primary) continue;
            sb.AppendLine($"    Asset {{");
            sb.AppendLine($"        id: \"{Esc(asset.Id)}\"  name: \"{Esc(asset.Name)}\"  src: \"{Esc(asset.Src)}\"");
            sb.AppendLine($"        pos: {F(asset.PosX)}, {F(asset.PosY)}, {F(asset.PosZ)}");
            sb.AppendLine($"        rot: {F(asset.RotX)}, {F(asset.RotY)}, {F(asset.RotZ)}");
            sb.AppendLine($"        scale: {F(asset.ScaleX)}, {F(asset.ScaleY)}, {F(asset.ScaleZ)}");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ── Deserialize (auto-detects format) ────────────────────────────────

    public static AnimationProjectData? Deserialize(string sml)
    {
        SmlDocument doc;
        try
        {
            doc = new SmlParser(sml).ParseDocument();
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("AnimationSerializer", "Failed to parse file.", ex);
            return null;
        }

        foreach (var node in doc.Roots)
        {
            if (string.Equals(node.Name, "Scene", StringComparison.OrdinalIgnoreCase))
                return ParseSceneFormat(node);
        }

        RunnerLogger.Warn("AnimationSerializer", "No Scene root node found.");
        return null;
    }

    // ── .scene format parser ──────────────────────────────────────────────

    private static AnimationProjectData ParseSceneFormat(SmlNode scene)
    {
        var assets    = new List<SceneAssetData>();
        var keyframes = new SortedDictionary<int, Dictionary<string, Quaternion>>();
        var fps         = 24;
        var totalFrames = 120;

        foreach (var child in scene.Children)
        {
            if (string.Equals(child.Name, "Character", StringComparison.OrdinalIgnoreCase))
            {
                var id    = GetString(child, "id",    "hero");
                var name  = GetString(child, "name",  id);
                var src   = GetString(child, "src",   string.Empty);
                var pos   = GetVec3f (child, "pos",   "posX", "posY", "posZ");
                var rot   = GetVec3f (child, "rot",   "rotX", "rotY", "rotZ");
                var scale = GetVec3f (child, "scale", "scaleX", "scaleY", "scaleZ", 1f);

                assets.Add(new SceneAssetData(id, name, src, true, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, scale.x, scale.y, scale.z));

                // Parse embedded Animation block
                foreach (var animChild in child.Children)
                {
                    if (!string.Equals(animChild.Name, "Animation", StringComparison.OrdinalIgnoreCase)) continue;
                    fps         = GetInt(animChild, "fps",         24);
                    totalFrames = GetInt(animChild, "totalFrames", 120);
                    ParseKeyBlocks(animChild, keyframes);
                }
            }
            else if (string.Equals(child.Name, "Asset", StringComparison.OrdinalIgnoreCase))
            {
                var id    = GetString(child, "id",    $"asset{assets.Count}");
                var name  = GetString(child, "name",  id);
                var src   = GetString(child, "src",   string.Empty);
                var pos   = GetVec3f (child, "pos",   "posX", "posY", "posZ");
                var rot   = GetVec3f (child, "rot",   "rotX", "rotY", "rotZ");
                var scale = GetVec3f (child, "scale", "scaleX", "scaleY", "scaleZ", 1f);

                assets.Add(new SceneAssetData(id, name, src, false, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, scale.x, scale.y, scale.z));
            }
        }

        return new AnimationProjectData(fps, totalFrames, assets, keyframes);
    }

    private static void ParseKeyBlocks(SmlNode anim, SortedDictionary<int, Dictionary<string, Quaternion>> keyframes)
    {
        foreach (var kfNode in anim.Children)
        {
            if (!string.Equals(kfNode.Name, "Key", StringComparison.OrdinalIgnoreCase)
             && !string.Equals(kfNode.Name, "Keyframe", StringComparison.OrdinalIgnoreCase)) continue;

            var frame = GetInt(kfNode, "frame", -1);
            if (frame < 0) continue;

            var bones = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
            foreach (var boneNode in kfNode.Children)
            {
                if (!string.Equals(boneNode.Name, "Bone", StringComparison.OrdinalIgnoreCase)) continue;
                var bName = GetString(boneNode, "name", string.Empty);
                if (string.IsNullOrWhiteSpace(bName)) continue;
                var x = GetFloat(boneNode, "x", 0f);
                var y = GetFloat(boneNode, "y", 0f);
                var z = GetFloat(boneNode, "z", 0f);
                var w = GetFloat(boneNode, "w", 1f);
                bones[bName] = new Quaternion(x, y, z, w).Normalized();
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

    /// <summary>
    /// Reads a Vec3f property (e.g. <c>pos: 1.0, 2.0, 3.0</c>) or falls back
    /// to three separate float properties (e.g. <c>posX: 1  posY: 2  posZ: 3</c>).
    /// </summary>
    private static (float x, float y, float z) GetVec3f(
        SmlNode node, string vecKey,
        string xKey, string yKey, string zKey,
        float defaultVal = 0f)
    {
        if (node.TryGetProperty(vecKey, out var v))
        {
            if (v.Kind == SmlValueKind.Vec3f)
            {
                var vec = (SmlVec3f)v.Value;
                return ((float)vec.X, (float)vec.Y, (float)vec.Z);
            }
            // Scalar float/int (e.g. scale: 1.0)
            if (v.Kind == SmlValueKind.Float)
            {
                var s = (float)(double)v.Value;
                return (s, s, s);
            }
            if (v.Kind == SmlValueKind.Int)
            {
                var s = (float)(int)v.Value;
                return (s, s, s);
            }
        }
        // Fallback: posX/posY/posZ
        return (GetFloat(node, xKey, defaultVal), GetFloat(node, yKey, defaultVal), GetFloat(node, zKey, defaultVal));
    }

    private static string F(float v) =>
        v.ToString("G6", CultureInfo.InvariantCulture);

    private static string Esc(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
