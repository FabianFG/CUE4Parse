using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Niagara
{
    public class FNiagaraVariable : FNiagaraVariableBase
    {
        public byte[] VarData;

        public FNiagaraVariable(FAssetArchive Ar) : base(Ar)
        {
            VarData = Ar.ReadArray<byte>();
        }

        public FNiagaraVariable(FName name, FStructFallback typeDef, byte[] varData) : base(name, typeDef)
        {
            VarData = varData;
        }
    }
}