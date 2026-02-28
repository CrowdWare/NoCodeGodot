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

using System;

namespace Runtime.ThreeD;

/// <summary>
/// Strips well-known rig prefixes from bone names so that pose data is
/// portable across different skeleton naming conventions (Mixamo, Mixamo1,
/// Meshy / standard, Character Creator, Biped).
///
/// Examples:
///   mixamorig:Hips  →  Hips
///   mixamorig_Spine →  Spine
///   mixamo1:LeftArm →  LeftArm
///   mixamo1_RightLeg→  RightLeg
///   CC_Base_Head    →  Head
///   Bip01_Pelvis    →  Pelvis
/// </summary>
public static class BoneNameNormalizer
{
    // Prefixes are checked in order; first match wins.
    private static readonly string[] Prefixes =
    [
        "mixamorig:",
        "mixamorig_",
        "mixamo1:",
        "mixamo1_",
        "CC_Base_",
        "Bip01_",
        "Bip01 ",   // 3ds-Max Biped with space separator
    ];

    /// <summary>
    /// Returns <paramref name="boneName"/> with any known rig prefix stripped.
    /// Returns the original string unchanged if no prefix matches.
    /// </summary>
    public static string Normalize(string boneName)
    {
        if (string.IsNullOrEmpty(boneName)) return boneName;

        foreach (var prefix in Prefixes)
        {
            if (boneName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return boneName[prefix.Length..];
        }

        return boneName;
    }
}
