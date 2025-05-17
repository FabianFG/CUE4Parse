using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkLayer
{
    public uint LayerId { get; protected set; }
    public List<AkRTPC> RTPCs { get; private set; }
    public uint RTPCId { get; private set; }
    public byte RTPCType { get; private set; }
    public float RTPCCrossfadingDefaultValue { get; private set; }
    public List<AkAssociatedLayerChild> Associations { get; private set; }

    public AkLayer(FArchive Ar)
    {
        LayerId = Ar.Read<uint>();
        RTPCs = AkRTPC.ReadMultiple(Ar);
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
        public uint AssociatedChildId { get; private set; }
        public List<AkRTPCGraphPoint> GraphPoints { get; private set; } = [];

        public AkAssociatedLayerChild(FArchive Ar)
        {
            AssociatedChildId = Ar.Read<uint>();
            GraphPoints = AkRTPCGraphPoint.ReadMultiple(Ar);
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
