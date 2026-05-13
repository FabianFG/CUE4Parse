using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class CAkLayer
{
    public readonly uint LayerId;
    public readonly AkRtpc[] Rtpcs;
    public readonly uint RtpcId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkGameSyncType RtpcType;
    public readonly float RtpcCrossfadingDefaultValue;
    public readonly AkAssociatedLayerChild[] Associations;

    public CAkLayer(FWwiseArchive Ar)
    {
        LayerId = Ar.Read<uint>();
        Rtpcs = AkRtpc.ReadArray(Ar);
        RtpcId = Ar.Read<uint>();

        if (Ar.Version > 89)
        {
            RtpcType = Ar.Read<EAkGameSyncType>();
        }

        if (Ar.Version <= 59)
        {
            RtpcCrossfadingDefaultValue = Ar.Read<float>();
        }

        // CAkLayer::SetChildAssoc
        Associations = AkAssociatedLayerChild.ReadArray(Ar);
    }

    public readonly struct AkAssociatedLayerChild
    {
        public readonly uint AssociatedChildId;
        public readonly AkRtpcGraphPoint[] GraphPoints;

        public AkAssociatedLayerChild(FWwiseArchive Ar)
        {
            AssociatedChildId = Ar.Read<uint>();
            GraphPoints = AkRtpcGraphPoint.ReadArray(Ar);
        }

        public static AkAssociatedLayerChild[] ReadArray(FWwiseArchive Ar) =>
            Ar.ReadArray(Ar.Read<int>(), () => new AkAssociatedLayerChild(Ar));
    }
}
