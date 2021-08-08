using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Niagara
{
    public class FNiagaraVariableWithOffset : FNiagaraVariableBase
    {
        public int Offset;

        public FNiagaraVariableWithOffset(FAssetArchive Ar) : base(Ar)
        {
            Offset = Ar.Read<int>();
        }

        public FNiagaraVariableWithOffset(FName name, FStructFallback typeDef, int offset) : base(name, typeDef)
        {
            Offset = offset;
        }
    }
}