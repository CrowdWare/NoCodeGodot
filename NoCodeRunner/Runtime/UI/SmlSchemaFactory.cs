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
        schema.RegisterKnownNode("Menu");
        schema.RegisterKnownNode("MenuItem");
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

        return schema;
    }
}
