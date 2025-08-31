using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Niagara;

public class FNiagaraVariableBase : IUStruct
{
    public FName Name;
    public FStructFallback TypeDef;
    protected FStructFallback? FallbackStruct;

    public FNiagaraVariableBase(FAssetArchive Ar)
    {
        if (Ar.Game == EGame.GAME_HellLetLoose)
        {
            FallbackStruct = new FStructFallback(Ar);
            Name = FallbackStruct.GetOrDefault<FName>(nameof(Name));
            TypeDef  = FallbackStruct.GetOrDefault<FStructFallback>(nameof(TypeDef));
            return;
        }
        Name = Ar.ReadFName();
        TypeDef = new FStructFallback(Ar, "NiagaraTypeDefinition");
    }

    public FNiagaraVariableBase(FName name, FStructFallback typeDef)
    {
        Name = name;
        TypeDef = typeDef;
    }
}
