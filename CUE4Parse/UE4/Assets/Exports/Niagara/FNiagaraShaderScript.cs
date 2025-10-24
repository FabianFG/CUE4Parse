using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Niagara.NiagaraShader;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Niagara;

public class FNiagaraShaderScript
{
    public int NumPermutations;
    public Dictionary<int, int>? ShaderStageToPermutation;
    public FNiagaraCompileHash? BaseCompileHash;
    public bool bLoadedFromCookedMaterial;
    public FNiagaraShaderMap RenderingThreadShaderMap;

    public FNiagaraShaderScript(FArchive Ar)
    {
        var bCooked = Ar.ReadBoolean();
        NumPermutations = Ar.Read<int>();
        if (Ar.Game >= EGame.GAME_UE4_26 && Ar.Game < EGame.GAME_UE5_0)
        {
            ShaderStageToPermutation = Ar.ReadMap(Ar.Read<int>, Ar.Read<int>);
        }
        if (Ar.Game >= EGame.GAME_UE5_2)
        {
            BaseCompileHash = new FNiagaraCompileHash(Ar);
        }

        bLoadedFromCookedMaterial = bCooked;

        if (bCooked)
        {
            var bValid = Ar.ReadBoolean();

            if (bValid)
            {
                RenderingThreadShaderMap = new FNiagaraShaderMap();
                RenderingThreadShaderMap.Deserialize(new FMaterialResourceProxyReader(Ar, false));
            }
        }
    }
}
