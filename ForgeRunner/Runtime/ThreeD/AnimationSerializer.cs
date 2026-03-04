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
    SortedDictionary<int, Dictionary<string, Quaternion>>    Keyframes,
    Dictionary<string, string>?                              SceneProperties = null);

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
    public static string LastError { get; private set; } = string.Empty;

    private static bool TrySplitBoneKey(string key, out string characterId, out string boneName)
    {
        var sep = key.IndexOf(':');
        if (sep <= 0 || sep >= key.Length - 1)
        {
            characterId = string.Empty;
            boneName = key;
            return false;
        }

        characterId = key[..sep];
        boneName = key[(sep + 1)..];
        return true;
    }

    private static string BuildBoneKey(string characterId, string boneName) =>
        string.IsNullOrWhiteSpace(characterId) ? boneName : $"{characterId}:{boneName}";

    private static string ToIdentifierToken(string raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var chars = raw.Trim().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            var valid = char.IsLetterOrDigit(c) || c == '_';
            chars[i] = valid ? c : '_';
        }

        var token = new string(chars).Trim('_');
        if (string.IsNullOrWhiteSpace(token))
            token = fallback;
        if (char.IsDigit(token[0]))
            token = "_" + token;
        return token;
    }

    private static string MakeUniqueId(string desiredId, HashSet<string> usedIds)
    {
        var baseId = string.IsNullOrWhiteSpace(desiredId) ? "asset" : desiredId;
        var candidate = baseId;
        var suffix = 1;
        while (!usedIds.Add(candidate))
        {
            candidate = $"{baseId}_{suffix}";
            suffix++;
        }
        return candidate;
    }

    private static bool IsIdentifierLike(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!char.IsLetter(value[0]) && value[0] != '_') return false;
        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return true;
    }

    private static void ValidateProjectData(List<SceneAssetData> assets, SortedDictionary<int, Dictionary<string, Quaternion>> keyframes)
    {
        var characterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasDuplicateIds = false;

        foreach (var asset in assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Id))
            {
                RunnerLogger.Warn("AnimationSerializer", $"Scene asset '{asset.Name}' has empty id.");
                continue;
            }

            if (!IsIdentifierLike(asset.Id))
            {
                RunnerLogger.Warn("AnimationSerializer", $"Scene asset id '{asset.Id}' is not an identifier token (expected [A-Za-z_][A-Za-z0-9_]*).");
            }

            if (!allIds.Add(asset.Id))
            {
                hasDuplicateIds = true;
            }

            if (asset.Primary)
                characterIds.Add(asset.Id);
        }

        if (hasDuplicateIds)
        {
            RunnerLogger.Warn("AnimationSerializer", "Scene contains duplicate asset ids. IDs must be unique.");
        }

        foreach (var (frame, bones) in keyframes)
        {
            foreach (var key in bones.Keys)
            {
                string charId;
                string boneName;
                var scoped = TrySplitBoneKey(key, out charId, out boneName);
                if (!scoped)
                {
                    RunnerLogger.Warn("AnimationSerializer", $"Keyframe {frame}: legacy unscoped bone key '{key}' is not supported.");
                    continue;
                }

                if (!characterIds.Contains(charId))
                {
                    RunnerLogger.Warn("AnimationSerializer", $"Keyframe {frame}: bone key '{key}' references unknown character id '{charId}'.");
                }
            }
        }
    }

    // ── Serialize (.scene) ────────────────────────────────────────────────

    public static string Serialize(AnimationProjectData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Scene {");
        sb.AppendLine($"    fps: {data.Fps}");
        sb.AppendLine($"    totalFrames: {data.TotalFrames}");
        if (data.SceneProperties is not null)
        {
            var keys = new List<string>(data.SceneProperties.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var value = data.SceneProperties[key] ?? string.Empty;
                sb.AppendLine($"    {key}: \"{Esc(value)}\"");
            }
        }
        sb.AppendLine();
        var characters = data.SceneAssets.FindAll(a => a.Primary);
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rawIdCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in characters)
        {
            var raw = string.IsNullOrWhiteSpace(c.Id) ? string.Empty : c.Id;
            if (!rawIdCounts.ContainsKey(raw))
                rawIdCounts[raw] = 0;
            rawIdCounts[raw] = rawIdCounts[raw] + 1;
        }

        for (var ci = 0; ci < characters.Count; ci++)
        {
            var character = characters[ci];
            var normalizedCharId = ToIdentifierToken(character.Id, $"char{ci + 1}");
            var charId = MakeUniqueId(normalizedCharId, usedIds);
            sb.AppendLine("    Character {");
            sb.AppendLine($"        id: {charId}  name: \"{Esc(character.Name)}\"  src: \"{Esc(character.Src)}\"");
            sb.AppendLine($"        pos: {F(character.PosX)}, {F(character.PosY)}, {F(character.PosZ)}");
            sb.AppendLine($"        rot: {F(character.RotX)}, {F(character.RotY)}, {F(character.RotZ)}");
            sb.AppendLine($"        scale: {F(character.ScaleX)}, {F(character.ScaleY)}, {F(character.ScaleZ)}");

            sb.AppendLine();
            sb.AppendLine("        Animation {");
            foreach (var (frame, bones) in data.Keyframes)
            {
                var emitted = false;
                var frameLines = new List<string>();
                foreach (var (key, q) in bones)
                {
                    string scopedId;
                    string boneName;
                    var hasScope = TrySplitBoneKey(key, out scopedId, out boneName);
                    if (!hasScope)
                    {
                        // Legacy unscoped key is intentionally not serialized.
                        continue;
                    }

                    var rawId = string.IsNullOrWhiteSpace(character.Id) ? string.Empty : character.Id;
                    var rawIdUnique = !string.IsNullOrWhiteSpace(rawId)
                        && rawIdCounts.TryGetValue(rawId, out var rawCount)
                        && rawCount == 1;

                    var matchCanonicalId = string.Equals(scopedId, charId, StringComparison.OrdinalIgnoreCase);
                    var matchRawId = rawIdUnique && string.Equals(scopedId, rawId, StringComparison.OrdinalIgnoreCase);
                    if (!matchCanonicalId && !matchRawId)
                    {
                        continue;
                    }

                    frameLines.Add($"                Bone {{ name: \"{Esc(boneName)}\" x: {F(q.X)} y: {F(q.Y)} z: {F(q.Z)} w: {F(q.W)} }}");
                    emitted = true;
                }

                if (!emitted) continue;
                sb.AppendLine($"            Key {{ frame: {frame}");
                foreach (var line in frameLines)
                    sb.AppendLine(line);
                sb.AppendLine("            }");
            }
            sb.AppendLine("        }");

            sb.AppendLine("    }");
        }

        // Props (non-primary assets)
        foreach (var asset in data.SceneAssets)
        {
            if (asset.Primary) continue;
            var normalizedAssetId = ToIdentifierToken(asset.Id, $"asset_{Math.Abs(asset.Id.GetHashCode())}");
            var assetId = MakeUniqueId(normalizedAssetId, usedIds);
            sb.AppendLine($"    Asset {{");
            sb.AppendLine($"        id: {assetId}  name: \"{Esc(asset.Name)}\"  src: \"{Esc(asset.Src)}\"");
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
        LastError = string.Empty;

        SmlDocument doc;
        try
        {
            doc = new SmlParser(sml).ParseDocument();
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("AnimationSerializer", "Failed to parse file.", ex);
            LastError = ex.Message;
            return null;
        }

        foreach (var node in doc.Roots)
        {
            if (string.Equals(node.Name, "Scene", StringComparison.OrdinalIgnoreCase))
                return ParseSceneFormat(node);
        }

        RunnerLogger.Warn("AnimationSerializer", "No Scene root node found.");
        LastError = "No Scene root node found.";
        return null;
    }

    // ── .scene format parser ──────────────────────────────────────────────

    private static AnimationProjectData ParseSceneFormat(SmlNode scene)
    {
        var assets    = new List<SceneAssetData>();
        var keyframes = new SortedDictionary<int, Dictionary<string, Quaternion>>();
        var fps         = GetInt(scene, "fps", -1);
        var totalFrames = GetInt(scene, "totalFrames", -1);
        var sceneProperties = ReadSceneStringProperties(scene);
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in scene.Children)
        {
            if (string.Equals(child.Name, "Character", StringComparison.OrdinalIgnoreCase))
            {
                if (child.TryGetProperty("id", out var idValue) && idValue.Kind == SmlValueKind.String)
                {
                    RunnerLogger.Warn("AnimationSerializer", "Character.id is stored as string literal; identifier token is recommended.");
                }
                var rawId = GetString(child, "id", "hero");
                var id = MakeUniqueId(rawId, usedIds);
                if (!string.Equals(id, rawId, StringComparison.OrdinalIgnoreCase))
                {
                    RunnerLogger.Warn("AnimationSerializer", $"Duplicate character id '{rawId}' adjusted to '{id}'.");
                }
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
                    // Backward compatibility: old files stored timeline settings
                    // inside each Character.Animation block.
                    if (fps < 0)
                        fps = GetInt(animChild, "fps", 24);
                    if (totalFrames < 0)
                        totalFrames = GetInt(animChild, "totalFrames", 120);
                    ParseKeyBlocks(animChild, id, keyframes);
                }
            }
            else if (string.Equals(child.Name, "Asset", StringComparison.OrdinalIgnoreCase))
            {
                if (child.TryGetProperty("id", out var idValue) && idValue.Kind == SmlValueKind.String)
                {
                    RunnerLogger.Warn("AnimationSerializer", "Asset.id is stored as string literal; identifier token is recommended.");
                }
                var rawId = GetString(child, "id", $"asset{assets.Count}");
                var id = MakeUniqueId(rawId, usedIds);
                if (!string.Equals(id, rawId, StringComparison.OrdinalIgnoreCase))
                {
                    RunnerLogger.Warn("AnimationSerializer", $"Duplicate asset id '{rawId}' adjusted to '{id}'.");
                }
                var name  = GetString(child, "name",  id);
                var src   = GetString(child, "src",   string.Empty);
                var pos   = GetVec3f (child, "pos",   "posX", "posY", "posZ");
                var rot   = GetVec3f (child, "rot",   "rotX", "rotY", "rotZ");
                var scale = GetVec3f (child, "scale", "scaleX", "scaleY", "scaleZ", 1f);

                assets.Add(new SceneAssetData(id, name, src, false, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, scale.x, scale.y, scale.z));
            }
        }

        if (fps < 0) fps = 24;
        if (totalFrames < 0) totalFrames = 120;

        ValidateProjectData(assets, keyframes);
        return new AnimationProjectData(fps, totalFrames, assets, keyframes, sceneProperties);
    }

    private static Dictionary<string, string> ReadSceneStringProperties(SmlNode scene)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in scene.Properties)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (string.Equals(key, "fps", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(key, "totalFrames", StringComparison.OrdinalIgnoreCase)) continue;
            if (!key.StartsWith("ai", StringComparison.OrdinalIgnoreCase)) continue;

            var text = value.Kind switch
            {
                SmlValueKind.String => (string)value.Value,
                SmlValueKind.Identifier => (string)value.Value,
                _ => null
            };
            if (text is not null)
                result[key] = text;
        }

        return result;
    }

    private static void ParseKeyBlocks(SmlNode anim, string characterId, SortedDictionary<int, Dictionary<string, Quaternion>> keyframes)
    {
        foreach (var kfNode in anim.Children)
        {
            if (!string.Equals(kfNode.Name, "Key", StringComparison.OrdinalIgnoreCase)
             && !string.Equals(kfNode.Name, "Keyframe", StringComparison.OrdinalIgnoreCase)) continue;

            var frame = GetInt(kfNode, "frame", -1);
            if (frame < 0) continue;

            if (!keyframes.TryGetValue(frame, out var bones))
            {
                bones = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
                keyframes[frame] = bones;
            }
            foreach (var boneNode in kfNode.Children)
            {
                if (!string.Equals(boneNode.Name, "Bone", StringComparison.OrdinalIgnoreCase)) continue;
                var bName = GetString(boneNode, "name", string.Empty);
                if (string.IsNullOrWhiteSpace(bName)) continue;
                var x = GetFloat(boneNode, "x", 0f);
                var y = GetFloat(boneNode, "y", 0f);
                var z = GetFloat(boneNode, "z", 0f);
                var w = GetFloat(boneNode, "w", 1f);
                var key = BuildBoneKey(characterId, bName);
                bones[key] = new Quaternion(x, y, z, w).Normalized();
            }
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
            if (v.Kind == SmlValueKind.Vec3i)
            {
                var vec = (SmlVec3i)v.Value;
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

    private static string F(float v)
    {
        if (MathF.Abs(v) < 1e-9f)
            return "0.0";

        var s = v.ToString("0.#########", CultureInfo.InvariantCulture);
        return s.Contains('.') ? s : s + ".0";
    }

    private static string Esc(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
