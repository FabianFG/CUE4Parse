using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse.UE4.Assets.Exports.Niagara.NiagaraShader;

public class FNiagaraShaderMapPointerTable : FShaderMapPointerTable
{
    public string[] DIClassName;
    
    public override void LoadFromArchive(FMaterialResourceProxyReader Ar)
    {
        base.LoadFromArchive(Ar);

        DIClassName = Ar.ReadArray(() => Ar.ReadFString(false));
    }
}