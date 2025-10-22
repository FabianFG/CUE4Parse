using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework.Shader;

public class FComputeKernelShaderMapId
{
    public ERHIFeatureLevel FeatureLevel;
    public ulong ShaderCodeHash;
    public FShaderTypeDependency[] ShaderTypeDependencies;
    public FPlatformTypeLayoutParameters LayoutParams;

    public FComputeKernelShaderMapId(FMemoryImageArchive Ar)
    {
        FeatureLevel = Ar.Read<ERHIFeatureLevel>();
        Ar.Position = Ar.Position.Align(8);
        ShaderCodeHash = Ar.Read<ulong>();
        ShaderTypeDependencies = Ar.ReadArray(() => new FShaderTypeDependency(Ar));
        LayoutParams = new FPlatformTypeLayoutParameters(Ar);
    }
}
