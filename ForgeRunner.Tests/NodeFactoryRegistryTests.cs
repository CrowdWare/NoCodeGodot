using Godot;
using Runtime.Generated;
using Runtime.Sml;
using Runtime.UI;
using System.Collections.Generic;
using System.Linq;

namespace ForgeRunner.Tests;

/// <summary>
/// Verifies that every SML element documented in docs/SML/Elements/ is
/// registered in NodeFactoryRegistry so that no "No factory registered"
/// warnings can occur at runtime.
///
/// The SML constant below covers ALL registered elements and ALL documented
/// properties — even combinations that make no visual sense — so that the
/// parser and factory are exercised together.
/// </summary>
public class NodeFactoryRegistryTests
{
    // -----------------------------------------------------------------------
    // Full-coverage SML: every registered element + every documented property
    // -----------------------------------------------------------------------
    private const string AllElementsSml = """
        Window {
            id: testWindow
            title: "All Elements Test"
            size: 1920, 1080
            minSize: 800, 400
            extendToTitle: true
            borderless: false

            WindowDrag {
                id: drag
                height: 40
            }

            VBoxContainer {
                id: rootCol
                spacing: 4
                padding: 8
                bgColor: "#111122"
                borderColor: "#222244"
                borderWidth: 1
                borderRadius: 0
                sizeFlagsHorizontal: expandFill
                sizeFlagsVertical: expandFill

                Label {
                    id: lbl1
                    text: "Label text"
                    color: "#FFFFFF"
                    fontSize: 14
                    fontWeight: bold
                    shrinkH: true
                }

                RichTextLabel {
                    id: rtl1
                    text: "[b]RichTextLabel[/b]"
                    fontSize: 13
                    sizeFlagsHorizontal: expandFill
                }

                MarkdownLabel {
                    id: mdlbl1
                    text: "**MarkdownLabel**"
                }

                Markdown {
                    id: md1
                    text: "# Markdown heading"
                }

                LineEdit {
                    id: lineEdit1
                    text: "LineEdit value"
                    color: "#CCCCCC"
                    fontSize: 13
                    bgColor: "#1A1A2E"
                    borderColor: "#333355"
                    borderWidth: 1
                    borderRadius: 4
                    sizeFlagsHorizontal: expandFill
                }

                TextEdit {
                    id: textEdit1
                    text: "TextEdit content"
                    fontSize: 12
                    bgColor: "#1A1A2E"
                    sizeFlagsHorizontal: expandFill
                    height: 60
                }

                CodeEdit {
                    id: codeEdit1
                    text: "// code"
                    syntax: "sml"
                    fontSize: 12
                    sizeFlagsHorizontal: expandFill
                    height: 60
                }

                HBoxContainer {
                    id: buttonsRow
                    spacing: 4
                    alignment: begin

                    Button {
                        id: btn1
                        text: "Button"
                        color: "#FFFFFF"
                        fontSize: 13
                        fontWeight: bold
                        bgColor: "#28A9E0"
                        borderColor: "#1A7AAA"
                        borderWidth: 1
                        borderRadius: 4
                        shrinkH: true
                    }

                    LinkButton {
                        id: lnk1
                        text: "LinkButton"
                        color: "#28A9E0"
                        fontSize: 13
                        shrinkH: true
                    }

                    CheckBox {
                        id: chk1
                        text: "CheckBox"
                        shrinkH: true
                    }

                    CheckButton {
                        id: chkb1
                        text: "CheckButton"
                        shrinkH: true
                    }

                    OptionButton {
                        id: opt1
                        shrinkH: true
                    }

                    MenuButton {
                        id: mnub1
                        text: "MenuButton"
                        shrinkH: true
                    }

                    TextureButton {
                        id: txbtn1
                        width: 32
                        height: 32
                        shrinkH: true
                        shrinkV: true
                    }

                    ColorPickerButton {
                        id: cpbtn1
                        width: 48
                        height: 28
                        shrinkH: true
                    }
                }

                HBoxContainer {
                    id: visualRow
                    spacing: 8

                    ColorRect {
                        id: cr1
                        color: "#FF5500"
                        width: 32
                        height: 32
                        shrinkH: true
                        shrinkV: true
                    }

                    TextureRect {
                        id: tr1
                        src: "res://logo.png"
                        width: 32
                        height: 32
                        shrinkH: true
                        shrinkV: true
                    }

                    NinePatchRect {
                        id: npr1
                        width: 64
                        height: 32
                        shrinkH: true
                        shrinkV: true
                    }
                }

                HSeparator { id: hsep1 }

                HBoxContainer {
                    id: rangeRow
                    spacing: 8

                    HSlider {
                        id: hsl1
                        min: 0
                        max: 100
                        value: 50
                        sizeFlagsHorizontal: expandFill
                    }

                    VSlider {
                        id: vsl1
                        min: 0
                        max: 100
                        value: 33
                        height: 80
                        shrinkH: true
                    }

                    ProgressBar {
                        id: pb1
                        min: 0
                        max: 100
                        value: 75
                        sizeFlagsHorizontal: expandFill
                    }

                    TextureProgressBar {
                        id: tpb1
                        width: 64
                        height: 32
                        shrinkH: true
                    }

                    SpinBox {
                        id: sb1
                        min: 0
                        max: 100
                        value: 42
                        shrinkH: true
                    }

                    HScrollBar {
                        id: hsb1
                        min: 0
                        max: 100
                        value: 25
                        sizeFlagsHorizontal: expandFill
                    }

                    VScrollBar {
                        id: vsb1
                        min: 0
                        max: 100
                        value: 50
                        height: 80
                        shrinkH: true
                    }
                }

                HBoxContainer {
                    id: containersRow
                    spacing: 4
                    sizeFlagsVertical: expandFill

                    MarginContainer {
                        id: mc1
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        Label { text: "MarginContainer" }
                    }

                    CenterContainer {
                        id: cc1
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        Label { text: "CenterContainer" }
                    }

                    PanelContainer {
                        id: pc1
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        bgColor: "#1E2030"
                        borderColor: "#333355"
                        borderWidth: 1
                        borderRadius: 4
                        Label { text: "PanelContainer" }
                    }

                    ScrollContainer {
                        id: sc1
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        VBoxContainer {
                            Label { text: "Scrollable A" }
                            Label { text: "Scrollable B" }
                        }
                    }

                    GridContainer {
                        id: gc1
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        Label { text: "Grid A" }
                        Label { text: "Grid B" }
                        Label { text: "Grid C" }
                        Label { text: "Grid D" }
                    }

                    AspectRatioContainer {
                        id: arc1
                        sizeFlagsVertical: expandFill
                        Label { text: "AspectRatio" }
                    }

                    FoldableContainer {
                        id: foc1
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        Label { text: "FoldableContainer" }
                    }

                    VSeparator { id: vsep1 }
                }

                HFlowContainer {
                    id: hfc1
                    sizeFlagsHorizontal: expandFill
                    Label { text: "HFlow A" }
                    Label { text: "HFlow B" }
                    Label { text: "HFlow C" }
                }

                VFlowContainer {
                    id: vfc1
                    height: 60
                    sizeFlagsHorizontal: expandFill
                    Label { text: "VFlow A" }
                    Label { text: "VFlow B" }
                }

                HBoxContainer {
                    id: splitRow
                    spacing: 4
                    height: 80

                    HSplitContainer {
                        id: hsc1
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        Label { text: "HSplit Left" }
                        Label { text: "HSplit Right" }
                    }

                    VSplitContainer {
                        id: vsc1
                        width: 120
                        sizeFlagsVertical: expandFill
                        Label { text: "VSplit Top" }
                        Label { text: "VSplit Bottom" }
                    }
                }

                TabContainer {
                    id: tabs1
                    height: 100

                    VBoxContainer {
                        TabContainer.title: "Tab Alpha"
                        Label { text: "Content Alpha" }
                    }

                    VBoxContainer {
                        TabContainer.title: "Tab Beta"
                        Label { text: "Content Beta" }
                    }
                }

                MenuBar {
                    id: mb1
                    preferGlobalMenu: false

                    PopupMenu {
                        id: testMenu
                        title: "File"
                        Item { id: itemNew  text: "New"  }
                        Item { id: itemOpen text: "Open" }
                        Item { id: itemSave text: "Save" }
                    }
                }

                ItemList {
                    id: list1
                    height: 80
                    sizeFlagsHorizontal: expandFill
                }

                Tree {
                    id: tree1
                    height: 80
                    sizeFlagsHorizontal: expandFill
                    showGuides: true
                }

                Control {
                    id: spacer1
                    sizeFlagsVertical: expandFill
                }
            }
        }
        """;

    // -----------------------------------------------------------------------
    // Helper: DFS walk over the parsed AST, yielding every node name
    // -----------------------------------------------------------------------
    private static IEnumerable<string> CollectNodeNames(IEnumerable<SmlNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node.Name;
            foreach (var child in CollectNodeNames(node.Children))
                yield return child;
        }
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void AllDocumentedElements_AreRegisteredInFactory()
    {
        // Parse with the full production schema so all identifiers are valid
        var schema = SmlSchemaFactory.CreateDefault();
        var parser = new SmlParser(AllElementsSml, schema);
        var doc = parser.ParseDocument();

        // Collect factory registry
        var registry = new NodeFactoryRegistry();
        var registeredNames = registry.GetRegisteredNodeNames()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Sugar pseudo-elements that are intentionally NOT in the factory:
        // - Item / CheckItem / Separator / Toggle / data: collection items handled inline.
        // - PopupMenu: extends Window (not Control); built directly by SmlUiBuilder.
        var sugarElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Item", "CheckItem", "Separator", "Toggle", "data", "PopupMenu" };

        // Every SML node must be either a sugar element or have a factory entry
        var notRegistered = CollectNodeNames(doc.Roots)
            .Where(name => !sugarElements.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => !registeredNames.Contains(name))
            .OrderBy(n => n)
            .ToList();

        Assert.Empty(notRegistered);
    }

    [Fact]
    public void AllElementsSml_ParsesWithoutUnknownNodeWarnings()
    {
        var schema = SmlSchemaFactory.CreateDefault();
        var parser = new SmlParser(AllElementsSml, schema);
        var doc = parser.ParseDocument();

        var nodeWarnings = doc.Warnings
            .Where(w => w.Contains("Unknown") || w.Contains("skipped"))
            .ToList();

        Assert.Empty(nodeWarnings);
    }

    /// <summary>
    /// Spot-checks that specific elements are registered by name.
    /// Uses only the registry's name dictionary — does not instantiate any
    /// Godot control, so this is safe to run without a Godot engine.
    /// </summary>
    [Theory]
    [InlineData("LineEdit")]
    [InlineData("LinkButton")]
    [InlineData("HSeparator")]
    [InlineData("VSeparator")]
    [InlineData("CheckBox")]
    [InlineData("CheckButton")]
    [InlineData("ColorRect")]
    [InlineData("MarginContainer")]
    [InlineData("ScrollContainer")]
    [InlineData("GridContainer")]
    [InlineData("HSplitContainer")]
    [InlineData("VSplitContainer")]
    [InlineData("HScrollBar")]
    [InlineData("VScrollBar")]
    [InlineData("VSlider")]
    [InlineData("SpinBox")]
    [InlineData("NinePatchRect")]
    [InlineData("HFlowContainer")]
    [InlineData("VFlowContainer")]
    [InlineData("AspectRatioContainer")]
    [InlineData("RichTextLabel")]
    [InlineData("OptionButton")]
    [InlineData("MenuButton")]
    public void NodeFactoryRegistry_Element_IsRegistered(string elementName)
    {
        var registry = new NodeFactoryRegistry();
        var registeredNames = registry.GetRegisteredNodeNames()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(elementName, registeredNames);
    }
}
