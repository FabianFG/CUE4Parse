using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyAudioBus : AbstractHierarchy
{
    public uint OverrideBusId { get; protected set; }
    public uint DeviceSharesetId { get; protected set; }

    public HierarchyAudioBus(FArchive Ar) : base(Ar)
    {
        OverrideBusId = Ar.Read<uint>();
        if (WwiseVersions.WwiseVersion > 126 && OverrideBusId == 0)
        {
            DeviceSharesetId = Ar.Read<uint>();
        }

    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
}

