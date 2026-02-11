using Runtime.Sml;
using Xunit;

namespace SMLCore.Tests;

public class SmlParserTests
{
    [Fact]
    public void ParseDocument_WithNestedNodes_ParsesAstStructure()
    {
        const string text = """
        Window {
            title: "Hello"
            Row {
                Label { text: "A" }
                Label { text: "B" }
            }
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterKnownNode("Row");
        schema.RegisterKnownNode("Label");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        Assert.Single(doc.Roots);
        var window = doc.Roots[0];
        Assert.Equal("Window", window.Name);
        Assert.Equal("Hello", window.GetRequiredProperty("title").AsStringOrThrow("title"));

        var row = Assert.Single(window.Children);
        Assert.Equal("Row", row.Name);
        Assert.Equal(2, row.Children.Count);
        Assert.Equal("A", row.Children[0].GetRequiredProperty("text").AsStringOrThrow("text"));
        Assert.Equal("B", row.Children[1].GetRequiredProperty("text").AsStringOrThrow("text"));
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_WithIdentifierProperty_ParsesUnquotedIdentifier()
    {
        const string text = """
        Window {
            id: main
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterIdentifierProperty("id");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var id = doc.Roots[0].GetRequiredProperty("id").AsStringOrThrow("id");
        Assert.Equal("main", id);
    }

    [Fact]
    public void ParseDocument_WithEnumProperty_MapsToInt()
    {
        const string text = """
        MenuItem {
            action: closeQuery
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("MenuItem");
        schema.RegisterEnumValue("action", "closeQuery", 1);

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var action = doc.Roots[0].GetRequiredProperty("action");
        Assert.Equal(1, action.AsEnumIntOrThrow("action"));
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_WithUnknownEnum_AddsWarning()
    {
        const string text = """
        MenuItem {
            action: doesNotExist
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("MenuItem");
        schema.RegisterEnumValue("action", "closeQuery", 1);

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        Assert.Single(doc.Warnings);
        Assert.Contains("Unknown enum value", doc.Warnings[0]);
    }

    [Fact]
    public void ParseDocument_WithUnknownNode_AddsWarningAndKeepsNode()
    {
        const string text = """
        UnknownWidget {
            text: "x"
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.WarnOnUnknownNodes = true;

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        Assert.Single(doc.Roots);
        Assert.Equal("UnknownWidget", doc.Roots[0].Name);
        Assert.Single(doc.Warnings);
        Assert.Contains("Unknown node", doc.Warnings[0]);
    }

    [Fact]
    public void ParseDocument_WithFloatValue_Throws()
    {
        const string text = """
        Window {
            percent: 0.9
        }
        """;

        var parser = new SmlParser(text);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("Float values are not supported", ex.Message);
    }

    [Fact]
    public void ParseDocument_WithUnregisteredUnquotedIdentifier_Throws()
    {
        const string text = """
        Window {
            title: hello
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("Unquoted identifier", ex.Message);
    }

    [Fact]
    public void ParseDocument_WithTupleValues_ParsesVec2AndVec3()
    {
        const string text = """
        Window {
            pos: 20,20
            Object3D {
                pos3D: 0,1200,0
            }
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterKnownNode("Object3D");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var pos = (SmlVec2i)doc.Roots[0].GetRequiredProperty("pos").Value;
        var pos3D = (SmlVec3i)doc.Roots[0].Children[0].GetRequiredProperty("pos3D").Value;

        Assert.Equal((20, 20), (pos.X, pos.Y));
        Assert.Equal((0, 1200, 0), (pos3D.X, pos3D.Y, pos3D.Z));
    }

    [Fact]
    public void ParseDocument_WithMultilineString_PreservesLineBreaks()
    {
        const string text = "Window {\n    text: \"\nfirst line\nsecond line\n\"\n}";

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var value = doc.Roots[0].GetRequiredProperty("text").AsStringOrThrow("text");
        Assert.Equal("\nfirst line\nsecond line\n", value);
    }

    [Fact]
    public void ParseDocument_WithIdAndAction_ParsesIdentifierAndEnumTogether()
    {
        const string text = """
        MenuItem {
            id: main
            action: closeQuery
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("MenuItem");
        schema.RegisterIdentifierProperty("id");
        schema.RegisterEnumValue("action", "closeQuery", 1);

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var node = doc.Roots[0];
        Assert.Equal("main", node.GetRequiredProperty("id").AsStringOrThrow("id"));
        Assert.Equal(1, node.GetRequiredProperty("action").AsEnumIntOrThrow("action"));
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_WithAnchorsPipeSyntax_ParsesAsStringValue()
    {
        const string text = """
        Window {
            Panel {
                anchors: top | bottom | left
            }
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterKnownNode("Panel");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var panel = Assert.Single(doc.Roots[0].Children);
        Assert.Equal("top,bottom,left", panel.GetRequiredProperty("anchors").AsStringOrThrow("anchors"));
    }

    [Fact]
    public void ParseDocument_WithPaddingSingleValue_ParsesAllSides()
    {
        const string text = """
        Panel {
            padding: 8
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Panel");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var padding = doc.Roots[0].GetRequiredProperty("padding").AsPaddingOrThrow("padding");
        Assert.Equal((8, 8, 8, 8), (padding.Top, padding.Right, padding.Bottom, padding.Left));
    }

    [Fact]
    public void ParseDocument_WithPaddingTwoValues_ParsesVerticalHorizontal()
    {
        const string text = """
        Panel {
            padding: 12,24
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Panel");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var padding = doc.Roots[0].GetRequiredProperty("padding").AsPaddingOrThrow("padding");
        Assert.Equal((12, 24, 12, 24), (padding.Top, padding.Right, padding.Bottom, padding.Left));
    }

    [Fact]
    public void ParseDocument_WithPaddingFourValues_ParsesTopRightBottomLeft()
    {
        const string text = """
        Panel {
            padding: 1,2,3,4
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Panel");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var padding = doc.Roots[0].GetRequiredProperty("padding").AsPaddingOrThrow("padding");
        Assert.Equal((1, 2, 3, 4), (padding.Top, padding.Right, padding.Bottom, padding.Left));
    }

    [Fact]
    public void ParseDocument_WithPaddingThreeValues_ThrowsMeaningfulError()
    {
        const string text = """
        Panel {
            padding: 8,8,8
        }
        """;

        var parser = new SmlParser(text);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("padding", ex.Message);
        Assert.Contains("1, 2, or 4", ex.Message);
        Assert.Contains("3 values are not supported", ex.Message);
    }
}
