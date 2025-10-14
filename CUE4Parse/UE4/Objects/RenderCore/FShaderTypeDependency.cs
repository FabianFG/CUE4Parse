using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RenderCore;

public class FShaderTypeDependency
{
    public FHashedName ShaderTypeName;
    public FSHAHash SourceHash;
    public int PermutationId;

    public FShaderTypeDependency(FArchive Ar)
    {
        ShaderTypeName = new FHashedName(Ar);
        SourceHash = new FSHAHash(Ar);
        if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.ShaderPermutationId)
        {
            PermutationId = Ar.Read<int>();
        }
    }

    public FShaderTypeDependency(FMemoryImageArchive Ar)
    {
        ShaderTypeName = new FHashedName(Ar);
        if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.ShaderPermutationId)
        {
            PermutationId = Ar.Read<int>();
        }
        SourceHash = new FSHAHash(Ar);
    }
}
