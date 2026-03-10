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

namespace Forge.Ai.Util;

public static class VersionedOutputPath
{
    public static string Resolve(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath) || !rawPath.Contains("<version>", StringComparison.Ordinal))
        {
            return rawPath;
        }

        var full = Path.GetFullPath(rawPath);
        var directory = Path.GetDirectoryName(full) ?? Directory.GetCurrentDirectory();
        var fileName = Path.GetFileName(full);
        var prefix = fileName.Replace("<version>", string.Empty, StringComparison.Ordinal);
        var stem = Path.GetFileNameWithoutExtension(prefix);
        var extension = Path.GetExtension(prefix);

        Directory.CreateDirectory(directory);

        var maxVersion = 0;
        var existing = Directory.GetFiles(directory, stem + "_*" + extension);
        foreach (var file in existing)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var idx = name.LastIndexOf('_');
            if (idx >= 0 && idx < name.Length - 1 && int.TryParse(name[(idx + 1)..], out var version))
            {
                maxVersion = Math.Max(maxVersion, version);
            }
        }

        return Path.Combine(directory, $"{stem}_{maxVersion + 1}{extension}");
    }
}
