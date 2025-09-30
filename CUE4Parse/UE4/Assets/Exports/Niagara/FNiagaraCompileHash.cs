using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Niagara;

public class FNiagaraCompileHash
{
    public byte[] DataHash;

    public FNiagaraCompileHash(FArchive Ar)
    {
        DataHash = Ar.ReadArray<byte>();
    }
}