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

namespace Forge.Ai.Imaging;

public sealed record GrokImageEditRequest(
    string Prompt,
    string PoseImagePath,
    string OutputPath,
    string Model = "grok-imagine-image",
    string? StyleImagePath = null,
    string? ExtraImagePath = null,
    string? NegativePrompt = null,
    double? ImageStrength = null,
    double? GuidanceScale = null,
    int? Steps = null,
    string? AspectRatio = null,
    string? Resolution = null,
    double? StyleStrength = null);

public sealed record GrokImageEditResult(
    string OutputPath,
    string SourceUrl,
    string Model);
