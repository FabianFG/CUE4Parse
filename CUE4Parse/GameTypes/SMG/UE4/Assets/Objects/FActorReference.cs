using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.SMG.UE4.Assets.Objects;

public class FActorReference : IUStruct
{
    public bool bIsAlias;
    public string ActorName;
    public byte Type;
    public byte SelectedType;

    public FActorReference(FAssetArchive Ar)
    {
        bIsAlias = Ar.ReadBoolean();
        ActorName = Ar.ReadFString();
        Type = Ar.Read<byte>();
        SelectedType = Ar.Read<byte>();
    }
}
