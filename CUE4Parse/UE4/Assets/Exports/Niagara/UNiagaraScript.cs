using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Niagara;

public class UNiagaraScript : UNiagaraScriptBase
{
    public FNiagaraShaderScript[]? LoadedScriptResources;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position == validPos)
            return;

        if (Ar is { Game: >= EGame.GAME_UE4_25, Owner.Provider.ReadShaderMaps: true })
        {
            try
            {
                LoadedScriptResources = Ar.ReadArray(() => new FNiagaraShaderScript(Ar));
            }
            finally
            {
                Ar.Position = validPos;
            }
        }
        else
        {
            Ar.Position = validPos;
        }
    }
}
