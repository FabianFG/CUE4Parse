using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Niagara;

public class FNiagaraVariableWithOffset : FNiagaraVariableBase
{
    public int Offset;

    public FNiagaraVariableWithOffset(FAssetArchive Ar) : base(Ar)
    {
        if (Ar.Game == EGame.GAME_HellLetLoose && FallbackStruct is not null)
        {
            Offset = FallbackStruct.GetOrDefault<int>(nameof(Offset));
            return;
        }
        Offset = Ar.Read<int>();
    }

    public FNiagaraVariableWithOffset(FName name, FStructFallback typeDef, int offset) : base(name, typeDef)
    {
        Offset = offset;
    }
}
