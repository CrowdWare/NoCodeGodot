using Runtime.Generated;

namespace ForgeRunner.Tests;

public class SchemaTests
{
    [Fact]
    public void SchemaProperties_Loads_WithoutErrors()
    {
        // Verifies the generated schema file loads without errors.
        Assert.NotNull(SchemaProperties.All);
    }
}