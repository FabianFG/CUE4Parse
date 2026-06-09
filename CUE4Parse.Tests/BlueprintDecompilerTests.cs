using CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;

namespace CUE4Parse.Tests;

public class BlueprintDecompilerTests
{
    [Fact]
    public void GetLineExpression_NullExpression_ReturnsEmptyAndDoesNotThrow()
    {
        Assert.Equal("", BlueprintDecompilerUtils.GetLineExpression(null));
    }
}
