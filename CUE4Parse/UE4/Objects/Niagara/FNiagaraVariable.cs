using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Niagara;

public class FNiagaraVariable : FNiagaraVariableBase
{
    public byte[] VarData;

    public FNiagaraVariable(FAssetArchive Ar) : base(Ar)
    {
        if (Ar.Game == EGame.GAME_FinalFantasy7Rebirth) Ar.Position += 8;
        if (Ar.Game == EGame.GAME_HellLetLoose && FallbackStruct is not null)
        {
            VarData =  FallbackStruct.GetOrDefault<byte[]>(nameof(VarData));
            return;
        }

        VarData = Ar.ReadArray<byte>();
        if (Ar.Game == EGame.GAME_FinalFantasy7Rebirth) Ar.Position += 4;
    }

    public FNiagaraVariable(FName name, FStructFallback typeDef, byte[] varData) : base(name, typeDef)
    {
        VarData = varData;
    }
}
