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

    public FNiagaraVariableBase(FAssetArchive Ar) : this(Ar, "NiagaraVariableBase") { }

    public FNiagaraVariableBase(FAssetArchive Ar, string? structName)
    {
        if (FNiagaraCustomVersion.Get(Ar) < FNiagaraCustomVersion.Type.VariablesUseTypeDefRegistry)
        {
            FallbackStruct = new FStructFallback(Ar, structName);
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
