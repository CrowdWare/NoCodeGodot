/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of NoCodeRunner.
 *
 *  NoCodeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  NoCodeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with NoCodeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

// Markdown V1 keyword list for runtime code highlighting.
// The runtime parser reads quoted strings from this file.
internal static class MarkdownSyntaxRules
{
    internal static readonly string[] Keywords =
    {
        "#",
        "##",
        "###",
        "*",
        "**",
        "```",
        "-",
        "[",
        "]",
        "(",
        ")"
    };
}