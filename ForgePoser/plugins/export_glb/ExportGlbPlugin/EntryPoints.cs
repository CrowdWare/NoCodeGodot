/*
#############################################################################
# Copyright (C) 2026 CrowdWare
#
# This file is part of Forge.
#
# SPDX-License-Identifier: GPL-3.0-or-later OR LicenseRef-CrowdWare-Commercial
#
# Forge is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# Forge is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with Forge. If not, see <https://www.gnu.org/licenses/>.
#
# Commercial licensing is available from CrowdWare for proprietary use.
#############################################################################
*/

using System;
using System.IO;

namespace ExportGlbPlugin;

public static class EntryPoints
{
    public static string ResolveOutputPath(string savePath)
    {
        if (string.IsNullOrWhiteSpace(savePath))
        {
            return savePath;
        }

        var normalized = savePath.Trim();
        var ext = Path.GetExtension(normalized);
        if (!string.Equals(ext, ".glb", StringComparison.OrdinalIgnoreCase))
        {
            normalized += ".glb";
        }

        return normalized;
    }

    public static bool GetIncludeAnimation(string projectPath)
    {
        return true;
    }

    public static bool GetIncludeProps(string projectPath)
    {
        return true;
    }

    public static bool GetAnimationOnlyCharacter(string projectPath)
    {
        return false;
    }

    public static string DescribePreset(string projectPath)
    {
        return "GLB preset: include animation + props";
    }
}
