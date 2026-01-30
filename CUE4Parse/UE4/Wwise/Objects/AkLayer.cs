using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkLayer
{
    public readonly uint LayerId;
    public readonly AkRtpc[] RTPCs;
    public readonly uint RTPCId;
    public readonly byte RTPCType;
    public readonly float RTPCCrossfadingDefaultValue;
    public readonly AkAssociatedLayerChild[] Associations;

    public AkLayer(FArchive Ar)
    {
        LayerId = Ar.Read<uint>();
        RTPCs = AkRtpc.ReadArray(Ar);
        RTPCId = Ar.Read<uint>();

        if (WwiseVersions.Version > 89)
        {
            RTPCType = Ar.Read<byte>();
        }

        if (WwiseVersions.Version <= 59)
        {
            RTPCCrossfadingDefaultValue = Ar.Read<float>();
        }

        // CAkLayer::SetChildAssoc
        Associations = AkAssociatedLayerChild.ReadArray(Ar);
    }

    public readonly struct AkAssociatedLayerChild
    {
        public readonly uint AssociatedChildId;
        public readonly AkRtpcGraphPoint[] GraphPoints;

        public AkAssociatedLayerChild(FArchive Ar)
        {
            AssociatedChildId = Ar.Read<uint>();
            GraphPoints = AkRtpcGraphPoint.ReadArray(Ar);
        }

        public static AkAssociatedLayerChild[] ReadArray(FArchive Ar) =>
            Ar.ReadArray(Ar.Read<int>(), () => new AkAssociatedLayerChild(Ar));
    }
}
