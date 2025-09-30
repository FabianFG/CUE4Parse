using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Niagara.NiagaraShader;

public class FNiagaraShaderMapContent : FShaderMapContent
{
    public FNiagaraShaderMapId ShaderMapId;
    
    public override void Deserialize(FMemoryImageArchive Ar)
    {
        base.Deserialize(Ar);

        ShaderMapId = new FNiagaraShaderMapId(Ar);
    }
}