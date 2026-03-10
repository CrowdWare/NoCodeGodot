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

public sealed record ForgeAiClientOptions(
    string ApiKey,
    string BaseUrl = "https://api.x.ai/v1")
{
    public static ForgeAiClientOptions FromEnvironment(string envVar = "GROK_API_KEY", string? baseUrl = null)
    {
        var key = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(key) && !string.Equals(envVar, "XAI_API_KEY", StringComparison.Ordinal))
        {
            key = Environment.GetEnvironmentVariable("XAI_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ForgeAiException($"Missing API key environment variable '{envVar}' (or fallback 'XAI_API_KEY').");
        }

        return new ForgeAiClientOptions(key, baseUrl ?? "https://api.x.ai/v1");
    }
}
