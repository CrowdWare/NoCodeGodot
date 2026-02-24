using Godot;
using Runtime.Generated;
using Runtime.Sml;
using Runtime.UI;
using System.Linq;

namespace ForgeRunner.Tests;

public class DockingContainerBgColorTests
{
    [Fact]
    public void NodePropertyMapper_BgColor_SetsMetaValue()
    {
        var mapper = new NodePropertyMapper();
        var control = new PanelContainer();

        mapper.Apply(control, "bgColor", SmlValue.FromString("#1C1E24"));

        Assert.True(control.HasMeta("bgColor"));
        Assert.Equal("#1C1E24", control.GetMeta("bgColor").AsString());
    }

    [Fact]
    public void ResolveBackgroundColor_UsesMetaWhenPresent_AndFallbackWhenMissingOrInvalid()
    {
        var fallback = new Color(0.1f, 0.2f, 0.3f, 1f);

        var fromMeta = DockingContainerControl.ResolveBackgroundColor("#1C1E24", fallback);
        Assert.Equal(new Color("#1C1E24"), fromMeta);

        var fromMissingMeta = DockingContainerControl.ResolveBackgroundColor(null, fallback);
        Assert.Equal(fallback, fromMissingMeta);

        var fromInvalidMeta = DockingContainerControl.ResolveBackgroundColor("not-a-color", fallback);
        Assert.Equal(fallback, fromInvalidMeta);
    }

    [Fact]
    public void SchemaProperties_DockingContainer_ContainsBgColorProperty()
    {
        var def = SchemaProperties.All.FirstOrDefault(p => p.TypeName == "DockingContainer" && p.SmlName == "bgColor");
        Assert.NotNull(def);
        Assert.Equal("bgColor", def.GodotName);
        Assert.Equal("string", def.ValueType);
    }
}