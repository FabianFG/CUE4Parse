using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyLayerContainer : BaseHierarchy
{
    public readonly uint[] ChildIds;
    public readonly List<AkLayer> Layers;
    public readonly byte IsContinuousValidation;

    public HierarchyLayerContainer(FArchive Ar) : base(Ar)
    {
        ChildIds = new AkChildren(Ar).ChildIds;

        var numLayers = Ar.Read<uint>();
        Layers = new List<AkLayer>((int)numLayers);
        for (int i = 0; i < numLayers; i++)
        {
            Layers.Add(new AkLayer(Ar));
        }

        if (WwiseVersions.Version > 118)
        {
            IsContinuousValidation = Ar.Read<byte>();
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ChildIds");
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName("Layers");
        serializer.Serialize(writer, Layers);

        writer.WritePropertyName("IsContinuousValidation");
        writer.WriteValue(IsContinuousValidation != 0);

        writer.WriteEndObject();
    }
}
