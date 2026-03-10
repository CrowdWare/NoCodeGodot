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

namespace Forge.Ai.Video;

public sealed record GrokVideoStylizeRequest(
    string InputVideoPath,
    string Prompt,
    string OutputPath,
    string Model = "grok-imagine-video",
    string? NegativePrompt = null,
    int PollIntervalMs = 3000,
    int TimeoutSeconds = 600,
    string SubmitEndpoint = "videos/edits",
    string StatusEndpointTemplate = "videos/{id}");

public sealed record GrokVideoStylizeResult(
    string JobId,
    string Status,
    string OutputPath,
    string SourceUrl,
    string Model);
