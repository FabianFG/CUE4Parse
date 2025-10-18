using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Niagara.NiagaraShader;

public class FNiagaraShaderMapId
{
    public FGuid CompilerVersionID;
    public ERHIFeatureLevel FeatureLevel;
    public string[] AdditionalDefines;
    public string[] AdditionalVariables;
    public FSHAHash BaseCompileHash;
    public FSHAHash[] ReferencedCompileHashes;
    public FPlatformTypeLayoutParameters LayoutParams;
    public FShaderTypeDependency[] ShaderTypeDependencies;
    public bool bUsesRapidIterationParams = true;

    public FNiagaraShaderMapId(FMemoryImageArchive Ar)
    {
        CompilerVersionID = Ar.Read<FGuid>();
        FeatureLevel = Ar.Read<ERHIFeatureLevel>();
        Ar.Position = Ar.Position.Align(8);

        if (Ar.Game < EGame.GAME_UE5_6)
        {
            _ = Ar.Read<FGuid>(); // BaseScriptID_DEPRECATED
        }

        AdditionalDefines = Ar.ReadArray(Ar.ReadFString);
        AdditionalVariables = Ar.ReadArray(Ar.ReadFString);

        BaseCompileHash = new FSHAHash(Ar);
        Ar.Position = Ar.Position.Align(8);
        ReferencedCompileHashes = Ar.ReadArray(() => new FSHAHash(Ar));
        LayoutParams = new FPlatformTypeLayoutParameters(Ar);
        Ar.Position = Ar.Position.Align(8);
        ShaderTypeDependencies = Ar.ReadArray(() => new FShaderTypeDependency(Ar));
        bUsesRapidIterationParams = Ar.ReadBoolean();
    }
}
