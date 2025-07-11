using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkLayer
{
    public readonly uint LayerId;
    public readonly List<AkRtpc> RTPCs;
    public readonly uint RTPCId;
    public readonly byte RTPCType;
    public readonly float RTPCCrossfadingDefaultValue;
    public readonly List<AkAssociatedLayerChild> Associations;

    public AkLayer(FArchive Ar)
    {
        LayerId = Ar.Read<uint>();
        RTPCs = AkRtpc.ReadMultiple(Ar);
        RTPCId = Ar.Read<uint>();

        if (WwiseVersions.Version > 89)
        {
            RTPCType = Ar.Read<byte>();
        }

        if (WwiseVersions.Version <= 59)
        {
            RTPCCrossfadingDefaultValue = Ar.Read<float>();
        }

        Associations = AkAssociatedLayerChild.ReadMultiple(Ar);
    }

    public class AkAssociatedLayerChild
    {
        public readonly uint AssociatedChildId;
        public readonly List<AkRtpcGraphPoint> GraphPoints = [];

        public AkAssociatedLayerChild(FArchive Ar)
        {
            AssociatedChildId = Ar.Read<uint>();
            GraphPoints = AkRtpcGraphPoint.ReadMultiple(Ar);
        }

        public static List<AkAssociatedLayerChild> ReadMultiple(FArchive Ar)
        {
            uint numAssociations = Ar.Read<uint>();
            var associations = new List<AkAssociatedLayerChild>((int)numAssociations);
            for (int j = 0; j < numAssociations; j++)
            {
                associations.Add(new AkAssociatedLayerChild(Ar));
            }

            return associations;
        }
    }
}
