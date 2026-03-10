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

func get_spec() -> Dictionary:
    return {
        "name": "context_properties",
        "rules": [
            {
                "parent": "TabContainer",
                "child": "*",
                "properties": [
                    {
                        "sml": "title",
                        "type": "string",
                        "targetMeta": "tabTitle",
                        "description": "Tab title read by the parent TabContainer. Use attached property syntax: `<containerId>.title: \"Caption\"` or `TabContainer.title: \"Caption\"`."
                    }
                ]
            },
            {
                "parent": "DockingContainer",
                "child": "*",
                "properties": [
                    {
                        "sml": "title",
                        "type": "string",
                        "targetMeta": "tabTitle",
                        "description": "Tab title read by the parent DockingContainer. Use attached property syntax: `<containerId>.title: \"Caption\"` or `DockingContainer.title: \"Caption\"`."
                    }
                ]
            }
        ]
    }
