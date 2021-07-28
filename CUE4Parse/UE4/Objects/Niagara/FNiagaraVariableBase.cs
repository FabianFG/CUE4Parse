using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Niagara
{
    public class FNiagaraVariableBase : IUStruct
    {
        public FName Name;
        public FStructFallback TypeDef;

        public FNiagaraVariableBase(FAssetArchive Ar)
        {
            Name = Ar.ReadFName();
            TypeDef = new FStructFallback(Ar, "NiagaraTypeDefinition");
        }

        public FNiagaraVariableBase(FName name, FStructFallback typeDef)
        {
            Name = name;
            TypeDef = typeDef;
        }
    }
}