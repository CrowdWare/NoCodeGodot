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

namespace Forge.Ai.Core;

internal static class PromptComposer
{
    private const string StrictImageBasePrompt =
        "SYSTEM RULES (MANDATORY): Preserve source pose/arrangement, camera framing, scene composition, object proportions, and left-right orientation. " +
        "Do not mirror or swap sides. Preserve subject identity and geometry consistency for all visible entities. Remove all UI/editor artifacts: " +
        "gizmos, handles, axis markers, bone overlays, helper meshes, labels, and text. Use style image only for mood, lighting, color palette, " +
        "and rendering style. Use extra image as material/reference where provided.";

    private const string StrictVideoBasePrompt =
        "SYSTEM RULES (MANDATORY): Preserve motion timing, camera framing, scene composition, object proportions, and left-right orientation " +
        "across all frames. Do not mirror or swap sides. Keep temporal consistency and subject identity. Remove UI/editor artifacts in every frame: " +
        "gizmos, handles, axis markers, bone overlays, helper meshes, labels, and text.";

    internal static string ComposeImagePrompt(string? userPrompt, string? negativePrompt)
    {
        return Compose(StrictImageBasePrompt, userPrompt, negativePrompt);
    }

    internal static string ComposeVideoPrompt(string? userPrompt, string? negativePrompt)
    {
        return Compose(StrictVideoBasePrompt, userPrompt, negativePrompt);
    }

    private static string Compose(string strictBasePrompt, string? userPrompt, string? negativePrompt)
    {
        var merged = strictBasePrompt;

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            merged += "\n\nUSER DIRECTIVES:\n" + userPrompt.Trim();
        }

        if (!string.IsNullOrWhiteSpace(negativePrompt))
        {
            merged += "\n\nAVOID:\n" + negativePrompt.Trim();
        }

        return merged;
    }
}
