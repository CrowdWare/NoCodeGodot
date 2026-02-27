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
        schema.RegisterIdProperty("id");

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
    public void ParseDocument_WithFloatValues_ParsesSuccessfully()
    {
        const string text = """
        Control {
            anchorLeft: 0.0
            anchorRight: 1.0
            width: 300
            opacity: 0.75
            anchorCenter: .5
        }
        """;

        var parser = new SmlParser(text);
        var doc = parser.ParseDocument();
        var node = Assert.Single(doc.Roots);

        Assert.Equal(SmlValueKind.Float, node.GetRequiredProperty("anchorLeft").Kind);
        Assert.Equal(0.0, node.GetRequiredProperty("anchorLeft").AsDoubleOrThrow("anchorLeft"), 10);

        Assert.Equal(SmlValueKind.Float, node.GetRequiredProperty("anchorRight").Kind);
        Assert.Equal(1.0, node.GetRequiredProperty("anchorRight").AsDoubleOrThrow("anchorRight"), 10);

        Assert.Equal(SmlValueKind.Int, node.GetRequiredProperty("width").Kind);
        Assert.Equal(300, node.GetRequiredProperty("width").AsIntOrThrow("width"));

        Assert.Equal(SmlValueKind.Float, node.GetRequiredProperty("opacity").Kind);
        Assert.Equal(0.75, node.GetRequiredProperty("opacity").AsDoubleOrThrow("opacity"), 10);

        Assert.Equal(SmlValueKind.Float, node.GetRequiredProperty("anchorCenter").Kind);
        Assert.Equal(0.5, node.GetRequiredProperty("anchorCenter").AsDoubleOrThrow("anchorCenter"), 10);
    }

    [Theory]
    [InlineData("1.")]
    [InlineData("1e-3")]
    [InlineData("12..3")]
    public void ParseDocument_WithInvalidFloatFormats_Throws(string invalidValue)
    {
        var text = $"Control {{\n    anchorLeft: {invalidValue}\n}}";

        var parser = new SmlParser(text);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("Invalid", ex.Message);
    }

    [Fact]
    public void ParseDocument_WithUnregisteredUnquotedIdentifier_AcceptsAsIdentifier()
    {
        // Unregistered properties accept unquoted identifiers — needed for component prop values
        // (e.g. tabId: tabStart). The runtime validates types, not the parser.
        const string text = """
        Window {
            title: hello
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var value = doc.Roots[0].GetRequiredProperty("title");
        Assert.Equal(SmlValueKind.Identifier, value.Kind);
        Assert.Equal("hello", value.AsStringOrThrow("title"));
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
        schema.RegisterIdProperty("id");
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

    [Fact]
    public void ParseDocument_WithQuotedId_Throws()
    {
        const string text = """
        Window {
            id: "main"
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterIdProperty("id");

        var parser = new SmlParser(text, schema);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("unquoted identifier symbol", ex.Message);
    }

    [Fact]
    public void ParseDocument_WithNumericId_ParsesAsIdentifierString()
    {
        const string text = """
        Window {
            id: 3
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterIdProperty("id");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();
        Assert.Equal("3", doc.Roots[0].GetRequiredProperty("id").AsStringOrThrow("id"));
    }

    [Fact]
    public void ParseDocument_WithDuplicateId_Throws()
    {
        const string text = """
        Window {
            id: main
            Panel {
                id: main
            }
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterKnownNode("Panel");
        schema.RegisterIdProperty("id");

        var parser = new SmlParser(text, schema);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("Duplicate id 'main'", ex.Message);
    }

    [Fact]
    public void ParseDocument_WithAttachedPropertyByTypeName_StoresInAttachedProperties()
    {
        const string text = """
        DockingContainer {
            Panel {
                DockingContainer.title: "MyTab"
            }
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("DockingContainer");
        schema.RegisterKnownNode("Panel");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var panel = Assert.Single(doc.Roots[0].Children);
        Assert.Empty(panel.Properties);
        Assert.True(panel.AttachedProperties.TryGetValue("DockingContainer", out var props));
        Assert.Equal("MyTab", props!["title"].AsStringOrThrow("title"));
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_WithAttachedPropertyByInstanceId_StoresInAttachedProperties()
    {
        const string text = """
        DockingContainer {
            id: dock
            Panel {
                dock.title: "MyTab"
            }
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("DockingContainer");
        schema.RegisterKnownNode("Panel");
        schema.RegisterIdProperty("id");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var panel = Assert.Single(doc.Roots[0].Children);
        Assert.Empty(panel.Properties);
        Assert.True(panel.AttachedProperties.TryGetValue("dock", out var props));
        Assert.Equal("MyTab", props!["title"].AsStringOrThrow("title"));
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_WithResourceRef_ParsesNamespaceAndPath()
    {
        const string text = """
        Strings {
            greeting: "Hello World"
        }
        Window {
            text: @Strings.greeting
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        // Strings block goes to Resources, not Roots
        Assert.Single(doc.Roots);
        Assert.Equal("Window", doc.Roots[0].Name);
        Assert.True(doc.Resources.ContainsKey("Strings"));

        // text property is a ResourceRef
        var textValue = doc.Roots[0].GetRequiredProperty("text");
        Assert.Equal(SmlValueKind.ResourceRef, textValue.Kind);
        var resRef = (SmlResourceRef)textValue.Value;
        Assert.Equal("Strings", resRef.Namespace);
        Assert.Equal("greeting", resRef.Path);
        Assert.Null(resRef.Fallback);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_WithResourceRefAndFallback_ParsesFallbackValue()
    {
        const string text = """
        Window {
            text: @Strings.missing, "Default Text"
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var textValue = doc.Roots[0].GetRequiredProperty("text");
        Assert.Equal(SmlValueKind.ResourceRef, textValue.Kind);
        var resRef = (SmlResourceRef)textValue.Value;
        Assert.Equal("Strings", resRef.Namespace);
        Assert.Equal("missing", resRef.Path);
        Assert.NotNull(resRef.Fallback);
        Assert.Equal(SmlValueKind.String, resRef.Fallback!.Kind);
        Assert.Equal("Default Text", resRef.Fallback.AsStringOrThrow("fallback"));
        // No warnings because fallback is provided
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_ResourceNamespaceBlock_GoesToResourcesNotRoots()
    {
        const string text = """
        Colors {
            primary: "#ff0000"
            secondary: "#0000ff"
        }
        Strings {
            ok: "OK"
        }
        Window {
            text: @Strings.ok
        }
        """;

        var parser = new SmlParser(text);
        var doc = parser.ParseDocument();

        Assert.Single(doc.Roots);
        Assert.Equal("Window", doc.Roots[0].Name);
        Assert.True(doc.Resources.ContainsKey("Colors"));
        Assert.True(doc.Resources.ContainsKey("Strings"));
        Assert.Equal("#ff0000", doc.Resources["Colors"].GetRequiredProperty("primary").AsStringOrThrow("primary"));
        Assert.Equal("OK", doc.Resources["Strings"].GetRequiredProperty("ok").AsStringOrThrow("ok"));
    }

    [Fact]
    public void ParseDocument_WithResourceRef_UnknownNamespaceNoFallback_AddsWarning()
    {
        const string text = """
        Window {
            text: @Missing.key
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        Assert.Single(doc.Warnings);
        Assert.Contains("@Missing", doc.Warnings[0]);
    }

    [Fact]
    public void ParseDocument_WithResourceRef_UnknownKeyNoFallback_AddsWarning()
    {
        const string text = """
        Strings {
            hello: "Hello"
        }
        Window {
            text: @Strings.doesNotExist
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        Assert.Single(doc.Warnings);
        Assert.Contains("@Strings.doesNotExist", doc.Warnings[0]);
    }

    [Fact]
    public void ParseDocument_WithResourceRef_UnknownNamespaceWithFallback_NoWarning()
    {
        const string text = """
        Window {
            text: @Strings.missing, "Fallback"
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_WithResourceRef_WellKnownNamespaceNoInlineBlock_NoWarning()
    {
        // @Strings.key without an inline Strings block should not warn —
        // well-known namespaces (Strings, Colors, etc.) are resolved from external files at runtime.
        const string text = """
        Window {
            title: @Strings.windowTitle
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ParseDocument_ResourceNamespaceBlock_NoWarnOnUnknownNode()
    {
        const string text = """
        Strings {
            hello: "Hello"
        }
        Window {
            text: @Strings.hello
        }
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.WarnOnUnknownNodes = true;

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        // No "Unknown node 'Strings'" warning
        Assert.Empty(doc.Warnings);
    }

    // ── User-Defined Components ──────────────────────────────────────────────

    [Fact]
    public void ParseDocument_WithInlineComponentDef_GoesToComponents()
    {
        const string text = """
        NavTab {
            property text: "Tab"
            property active: false

            Control {
                Label { text: {text} }
            }
        }
        Window {
            NavTab { text: "Start" }
        }
        """;

        var parser = new SmlParser(text);
        var doc = parser.ParseDocument();

        // Component does not go into Roots
        Assert.Single(doc.Roots);
        Assert.Equal("Window", doc.Roots[0].Name);

        // Component is registered
        Assert.True(doc.Components.ContainsKey("NavTab"));
        var comp = doc.Components["NavTab"];
        Assert.Equal("NavTab", comp.Name);
        Assert.Null(comp.Namespace);

        // Props
        Assert.Equal(2, comp.Props.Count);
        Assert.Equal("Tab", comp.Props["text"].AsStringOrThrow("text"));
        Assert.False(comp.Props["active"].AsBoolOrThrow("active"));

        // Body
        Assert.Equal("Control", comp.Body.Name);
    }

    [Fact]
    public void ParseDocument_WithComponentPropIdTypeHint_StoresAsIdentifier()
    {
        const string text = """
        NavTab {
            property tabId: id

            Control {
                Button { id: {tabId} }
            }
        }
        Window {}
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var comp = doc.Components["NavTab"];
        Assert.True(comp.Props.ContainsKey("tabId"));
        Assert.Equal(SmlValueKind.Identifier, comp.Props["tabId"].Kind);
        Assert.Equal("id", (string)comp.Props["tabId"].Value);
    }

    [Fact]
    public void ParseDocument_WithPropRef_ParsesAsPropRefValue()
    {
        const string text = """
        Card {
            property title: "Default"

            VBox {
                Label { text: {title} }
            }
        }
        Window {}
        """;

        var parser = new SmlParser(text);
        var doc = parser.ParseDocument();

        var comp = doc.Components["Card"];
        var label = comp.Body.Children[0];
        Assert.Equal("Label", label.Name);

        var textVal = label.GetRequiredProperty("text");
        Assert.Equal(SmlValueKind.PropRef, textVal.Kind);
        Assert.Equal("title", (string)textVal.Value);
    }

    [Fact]
    public void ParseDocument_WithComponentAndPropRefOnMultipleProps_SubstitutesAll()
    {
        const string text = """
        NavTab {
            property text:   "Tab"
            property tabId:  id
            property active: false

            Control {
                Button {
                    id: {tabId}
                    toggleMode: {active}
                }
                Label { text: {text} }
            }
        }
        Window {}
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var comp = doc.Components["NavTab"];
        Assert.Equal(3, comp.Props.Count);

        var button = comp.Body.Children[0];
        Assert.Equal(SmlValueKind.PropRef, button.GetRequiredProperty("id").Kind);
        Assert.Equal("tabId", (string)button.GetRequiredProperty("id").Value);
        Assert.Equal(SmlValueKind.PropRef, button.GetRequiredProperty("toggleMode").Kind);
        Assert.Equal("active", (string)button.GetRequiredProperty("toggleMode").Value);

        var label = comp.Body.Children[1];
        Assert.Equal(SmlValueKind.PropRef, label.GetRequiredProperty("text").Kind);
        Assert.Equal("text", (string)label.GetRequiredProperty("text").Value);
    }

    [Fact]
    public void ParseDocument_WithNamespacedComponent_SetsNamespaceAndName()
    {
        const string text = """
        ui.NavTab {
            property text: "Tab"

            Control {}
        }
        Window {}
        """;

        var parser = new SmlParser(text);
        var doc = parser.ParseDocument();

        // Component is keyed by short name
        Assert.True(doc.Components.ContainsKey("NavTab"));
        var comp = doc.Components["NavTab"];
        Assert.Equal("NavTab", comp.Name);
        Assert.Equal("ui", comp.Namespace);
    }

    [Fact]
    public void ParseDocument_ComponentWithNoBody_Throws()
    {
        const string text = """
        EmptyComp {
            property text: "x"
        }
        Window {}
        """;

        var parser = new SmlParser(text);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("no body element", ex.Message);
    }

    [Fact]
    public void ParseDocument_ComponentWithMultipleRootElements_Throws()
    {
        const string text = """
        BadComp {
            property x: ""
            Control {}
            Control {}
        }
        Window {}
        """;

        var parser = new SmlParser(text);
        var ex = Assert.Throws<SmlParseException>(() => parser.ParseDocument());
        Assert.Contains("exactly one root element", ex.Message);
    }

    [Fact]
    public void ParseDocument_ComponentWithInheritance_SynthesizesBodyFromBaseType()
    {
        const string text = """
        NavTab {
            property text:   "Tab"
            property tabId:  id
            inheritance:     Control

            shrinkH: true
            width: 120
            height: 44

            Label { text: {text} }
        }
        Window {}
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterKnownNode("Control");
        schema.RegisterKnownNode("Label");

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        var comp = doc.Components["NavTab"];

        // Body is the synthesized Control node
        Assert.Equal("Control", comp.Body.Name);

        // Props are on the component
        Assert.Equal(2, comp.Props.Count);
        Assert.Equal("Tab", comp.Props["text"].AsStringOrThrow("text"));

        // Default properties landed on the body
        Assert.Equal(120, comp.Body.GetRequiredProperty("width").AsIntOrThrow("width"));
        Assert.Equal(44,  comp.Body.GetRequiredProperty("height").AsIntOrThrow("height"));
        Assert.True(comp.Body.GetRequiredProperty("shrinkH").AsBoolOrThrow("shrinkH"));

        // inheritance: itself must NOT be on the body
        Assert.False(comp.Body.Properties.ContainsKey("inheritance"));

        // Children were moved into the body
        var label = Assert.Single(comp.Body.Children);
        Assert.Equal("Label", label.Name);
        Assert.Equal(SmlValueKind.PropRef, label.GetRequiredProperty("text").Kind);
    }

    [Fact]
    public void ParseDocument_ComponentWithInheritance_EmptyChildrenAllowed()
    {
        const string text = """
        PaddedBox {
            property size: 8
            inheritance: VBoxContainer
        }
        Window {}
        """;

        var parser = new SmlParser(text);
        var doc = parser.ParseDocument();

        Assert.True(doc.Components.ContainsKey("PaddedBox"));
        Assert.Equal("VBoxContainer", doc.Components["PaddedBox"].Body.Name);
        Assert.Empty(doc.Components["PaddedBox"].Body.Children);
    }

    [Fact]
    public void ParseDocument_ComponentWithInheritance_PropRefOnBodyProperty()
    {
        // Body properties themselves can be PropRefs
        const string text = """
        Spacer {
            property size: 8
            inheritance: Control

            width: {size}
            height: {size}
        }
        Window {}
        """;

        var parser = new SmlParser(text);
        var doc = parser.ParseDocument();

        var comp = doc.Components["Spacer"];
        Assert.Equal("Control", comp.Body.Name);
        Assert.Equal(SmlValueKind.PropRef, comp.Body.GetRequiredProperty("width").Kind);
        Assert.Equal("size", (string)comp.Body.GetRequiredProperty("width").Value);
        Assert.Equal(SmlValueKind.PropRef, comp.Body.GetRequiredProperty("height").Kind);
    }

    [Fact]
    public void ParseDocument_ComponentDef_DoesNotWarnAsUnknownNode()
    {
        const string text = """
        MyWidget {
            property label: "x"

            Control {}
        }
        Window {}
        """;

        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Window");
        schema.RegisterKnownNode("Control");
        schema.WarnOnUnknownNodes = true;

        var parser = new SmlParser(text, schema);
        var doc = parser.ParseDocument();

        // No "Unknown node 'MyWidget'" warning — it's a component definition
        Assert.DoesNotContain(doc.Warnings, w => w.Contains("'MyWidget'"));
    }
}
