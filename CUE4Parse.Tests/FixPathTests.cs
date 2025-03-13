using CUE4Parse.FileProvider;

namespace CUE4Parse.Tests;

public class FixPathTests
{
    private readonly IFileProvider _provider = new DummyFileProvider();

    [Fact]
    public void FixPath_ImplicitProject_NoExtension()
    {
        Assert.Equal("ProjectName/Content/Data/ProjectNameData.uasset", _provider.FixPath("/Game/Data/ProjectNameData"));
    }

    [Fact]
    public void FixPath_ImplicitProject_ExplicitExtension()
    {
        Assert.Equal("ProjectName/Content/Data/ProjectNameData.uexp", _provider.FixPath("/Game/Data/ProjectNameData.uexp"));
    }

    [Fact]
    public void FixPath_ExplicitProject_NoContent_NoExtension()
    {
        // when the project name is provided, all subdirectories are assumed to not need any fixing
        Assert.Equal("ProjectName/Character/DefaultCharacter.uasset", _provider.FixPath("ProjectName/Character/DefaultCharacter"));
    }

    [Fact]
    public void FixPath_EngineProject_NoContent_ExplicitExtension()
    {
        Assert.Equal("Engine/Content/BasicShapes/Cube.uasset", _provider.FixPath("Engine/BasicShapes/Cube.uasset"));
    }

    [Fact]
    public void FixPath_EngineProject_Config_ExplicitExtension()
    {
        Assert.Equal("Engine/Config/BaseInput.ini", _provider.FixPath("Engine/Config/BaseInput.ini"));
    }

    [Fact]
    public void FixPath_EngineProject_Config_NoExtension()
    {
        Assert.Equal("Engine/Config/BaseInput.uasset", _provider.FixPath("Engine/Config/BaseInput"));
    }
}
