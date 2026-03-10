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

extends RefCounted

# Declarative layout alias policy for SML.
# Canonical properties:
# - size (vec2)
# - position (vec2)
#
# Aliases are applied in source order (last write wins).
func get_spec() -> Dictionary:
    return {
        "name": "layout_aliases",
        "rules": [
            {
                "canonical": "size",
                "appliesTo": ["Control", "Window"],
                "aliases": [
                    {"name": "size", "mode": "whole"},
                    {"name": "width", "mode": "x"},
                    {"name": "height", "mode": "y"}
                ],
                "merge": "partial",
                "precedence": "last-write-wins"
            },
            {
                "canonical": "position",
                "appliesTo": ["Control", "Window"],
                "aliases": [
                    {"name": "position", "mode": "whole"},
                    {"name": "pos", "mode": "whole"},
                    {"name": "x", "mode": "x"},
                    {"name": "left", "mode": "x"},
                    {"name": "y", "mode": "y"},
                    {"name": "top", "mode": "y"}
                ],
                "merge": "partial",
                "precedence": "last-write-wins"
            }
        ]
    }