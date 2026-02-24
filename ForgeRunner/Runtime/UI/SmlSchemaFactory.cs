/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

using Runtime.Generated;
using Runtime.Sml;

namespace Runtime.UI;

public static class SmlSchemaFactory
{
    public static SmlParserSchema CreateDefault()
    {
        var schema = new SmlParserSchema();

        SchemaTypes.RegisterKnownNodes(schema);

        // Runtime pseudo nodes (not Godot classes) that are valid in SML trees.
        schema.RegisterKnownNode("Item");
        schema.RegisterKnownNode("CheckItem");
        schema.RegisterKnownNode("Separator");
        schema.RegisterKnownNode("Toggle");
        schema.RegisterKnownNode("data");

        schema.RegisterIdProperty("id");
        schema.RegisterIdentifierProperty("clicked");
        schema.RegisterEnumValue("action", "closeQuery", 1);
        schema.RegisterEnumValue("action", "open", 2);
        schema.RegisterEnumValue("action", "save", 3);
        schema.RegisterEnumValue("action", "saveAs", 4);
        schema.RegisterEnumValue("action", "animPlay", 10);
        schema.RegisterEnumValue("action", "animStop", 11);
        schema.RegisterEnumValue("action", "animRewind", 12);
        schema.RegisterEnumValue("action", "animScrub", 13);
        schema.RegisterEnumValue("action", "perspectiveNear", 14);
        schema.RegisterEnumValue("action", "perspectiveDefault", 15);
        schema.RegisterEnumValue("action", "perspectiveFar", 16);
        schema.RegisterEnumValue("action", "zoomIn", 17);
        schema.RegisterEnumValue("action", "zoomOut", 18);
        schema.RegisterEnumValue("action", "cameraReset", 19);
        schema.RegisterEnumValue("scaling", "layout", 1);
        schema.RegisterEnumValue("scaling", "fixed", 2);
        schema.RegisterEnumValue("scrollBarPosition", "right", 1);
        schema.RegisterEnumValue("scrollBarPosition", "left", 2);
        schema.RegisterEnumValue("scrollBarPosition", "bottom", 3);
        schema.RegisterEnumValue("scrollBarPosition", "top", 4);
        schema.RegisterEnumValue("sizeFlagsHorizontal", "shrinkBegin", 0);
        schema.RegisterEnumValue("sizeFlagsHorizontal", "fill", 1);
        schema.RegisterEnumValue("sizeFlagsHorizontal", "expand", 2);
        schema.RegisterEnumValue("sizeFlagsHorizontal", "expandFill", 3);
        schema.RegisterEnumValue("sizeFlagsHorizontal", "shrinkCenter", 4);
        schema.RegisterEnumValue("sizeFlagsHorizontal", "shrinkEnd", 8);

        schema.RegisterEnumValue("sizeFlagsVertical", "shrinkBegin", 0);
        schema.RegisterEnumValue("sizeFlagsVertical", "fill", 1);
        schema.RegisterEnumValue("sizeFlagsVertical", "expand", 2);
        schema.RegisterEnumValue("sizeFlagsVertical", "expandFill", 3);
        schema.RegisterEnumValue("sizeFlagsVertical", "shrinkCenter", 4);
        schema.RegisterEnumValue("sizeFlagsVertical", "shrinkEnd", 8);

        schema.RegisterEnumValue("dockSide", "farLeft", 1);
        schema.RegisterEnumValue("dockSide", "farLeftBottom", 2);
        schema.RegisterEnumValue("dockSide", "left", 3);
        schema.RegisterEnumValue("dockSide", "leftBottom", 4);
        schema.RegisterEnumValue("dockSide", "center", 5);
        schema.RegisterEnumValue("dockSide", "right", 6);
        schema.RegisterEnumValue("dockSide", "rightBottom", 7);
        schema.RegisterEnumValue("dockSide", "farRight", 8);
        schema.RegisterEnumValue("dockSide", "farRightBottom", 9);

        schema.RegisterEnumValue("mouseFilter", "stop", 0);
        schema.RegisterEnumValue("mouseFilter", "pass", 1);
        schema.RegisterEnumValue("mouseFilter", "ignore", 2);

        return schema;
    }
}
