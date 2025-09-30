using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Niagara;

public class UNiagaraScript : UNiagaraScriptBase
{
    public FNiagaraShaderScript[]? LoadedScriptResources;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position == validPos)
            return;

        LoadedScriptResources = Ar.ReadArray(() => new FNiagaraShaderScript(Ar));
    }
}