using CUE4Parse.FileProvider;

namespace CUE4Parse.Tests;

public class UnitTest
{
    private readonly IFileProvider _provider = new DummyFileProvider();

    [Fact]
    public void FixPath_Generic_NoExtension()
    {
        Assert.Equal("ProjectName/Content/Data/ProjectNameData.uasset", _provider.FixPath("/Game/Data/ProjectNameData"));
    }

    [Fact]
    public void FixPath_Generic_ExplicitExtension()
    {
        Assert.Equal("ProjectName/Content/Data/ProjectNameData.uexp", _provider.FixPath("/Game/Data/ProjectNameData.uexp"));
    }

    [Fact]
    public void FixPath_Generic_NoContent_NoExtension()
    {
        Assert.Equal("ProjectName/Content/Character/DefaultCharacter.uasset", _provider.FixPath("ProjectName/Character/DefaultCharacter"));
    }

    [Fact]
    public void FixPath_Engine_NoContent_ExplicitExtension()
    {
        Assert.Equal("Engine/Content/BasicShapes/Cube.uasset", _provider.FixPath("Engine/BasicShapes/Cube.uasset"));
    }

    [Fact]
    public void FixPath_Engine_Config_ExplicitExtension()
    {
        Assert.Equal("Engine/Config/BaseInput.ini", _provider.FixPath("Engine/Config/BaseInput.ini"));
    }

    [Fact]
    public void FixPath_Engine_Config_NoExtension()
    {
        Assert.Equal("Engine/Config/BaseInput.uasset", _provider.FixPath("Engine/Config/BaseInput"));
    }
}
