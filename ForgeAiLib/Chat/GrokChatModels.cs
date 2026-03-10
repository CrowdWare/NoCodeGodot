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

namespace Forge.Ai.Chat;

public sealed record GrokChatRequest(
    string Prompt,
    string? SystemPrompt = null,
    string Model = "grok-4",
    double Temperature = 0.0,
    int? MaxTokens = null);

public sealed record GrokChatResult(
    string Content,
    string Model,
    string? FinishReason);

public sealed record GrokImageAnalysisRequest(
    string ImagePath,
    string Prompt,
    string Model = "grok-4",
    int MaxTokens = 700);
