using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Niagara;

public class UNiagaraDataInterface : UObject;

public class UNiagaraDataInterfaceTexture : UNiagaraDataInterface
{
    public byte[] StreamData = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FNiagaraCustomVersion.Get(Ar) >= FNiagaraCustomVersion.Type.TextureDataInterfaceUsesCustomSerialize)
            StreamData = Ar.ReadArray<byte>();
    }
}
