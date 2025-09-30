using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Niagara.NiagaraShader;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Niagara;

public class FNiagaraShaderScript
{
    public int NumPermutations;
    public FNiagaraCompileHash BaseCompileHash;
    public bool bLoadedFromCookedMaterial;
    public FNiagaraShaderMap RenderingThreadShaderMap;
    
    public FNiagaraShaderScript(FArchive Ar)
    {
        var bCooked = Ar.ReadBoolean();
        NumPermutations = Ar.Read<int>();
        BaseCompileHash = new FNiagaraCompileHash(Ar);

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