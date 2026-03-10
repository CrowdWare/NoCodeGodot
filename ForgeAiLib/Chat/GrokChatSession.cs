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

using Forge.Ai.Core;

namespace Forge.Ai.Chat;

public sealed class GrokChatSession
{
    private readonly GrokChatService _service;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int? _maxTokens;
    private readonly List<(string Role, string Content)> _messages = [];

    public GrokChatSession(
        GrokChatService service,
        string model = "grok-4",
        double temperature = 0.0,
        int? maxTokens = 4096,
        string? systemPrompt = null)
    {
        _service = service;
        _model = model;
        _temperature = temperature;
        _maxTokens = maxTokens;

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            _messages.Add(("system", systemPrompt));
        }
    }

    public IReadOnlyList<(string Role, string Content)> Messages => _messages;

    public void ClearKeepSystemPrompt()
    {
        var system = _messages.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
        _messages.Clear();
        if (!string.IsNullOrWhiteSpace(system.Content))
        {
            _messages.Add(system);
        }
    }

    public async Task<string> AskAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new ForgeAiException("Prompt is required.");
        }

        _messages.Add(("user", userPrompt));

        var systemPrompt = _messages.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)).Content;
        var stitchedPrompt = string.Join(
            "\n\n",
            _messages.Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
                     .Select(m => $"{m.Role}: {m.Content}"));

        var result = await _service.CompleteAsync(new GrokChatRequest(
            Prompt: stitchedPrompt,
            SystemPrompt: systemPrompt,
            Model: _model,
            Temperature: _temperature,
            MaxTokens: _maxTokens), cancellationToken).ConfigureAwait(false);

        _messages.Add(("assistant", result.Content));
        return result.Content;
    }
}
