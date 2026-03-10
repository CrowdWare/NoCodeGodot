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

#pragma once

#include <string>
#include <vector>

namespace forge {

enum class BlockKind { Heading, Paragraph, CodeBlock, ListItem, HRule, Image };

struct MarkdownBlock {
    BlockKind   kind  = BlockKind::Paragraph;
    int         level = 0;     // heading level 1–6
    std::string text;          // inline content (may contain inline markup)
    std::string lang;          // fenced code block language hint
    std::string src;           // image source URL / path
    std::string alt;           // image alt text
};

// Parse markdown text into a flat list of blocks.
std::vector<MarkdownBlock> parse_markdown(const std::string& md);

// Convert inline markdown markup to Godot BBCode.
// Handles: **bold**, _italic_, `code`, [label](url)
std::string inline_to_bbcode(const std::string& text);

} // namespace forge
