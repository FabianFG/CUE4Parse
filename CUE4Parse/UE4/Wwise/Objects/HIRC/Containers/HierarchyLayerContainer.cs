using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyLayerContainer : BaseHierarchy
{
    public readonly uint[] ChildIds;
    public readonly CAkLayer[] Layers;
    public readonly bool IsContinuousValidation;

    // CAkBankMgr::StdBankRead<CAkLayerCntr>
    // CAkLayerCntr::SetInitialValues
    public HierarchyLayerContainer(FArchive Ar) : base(Ar)
    {
        ChildIds = new AkChildren(Ar).ChildIds;
        Layers = Ar.ReadArray((int) Ar.Read<uint>(), () => new CAkLayer(Ar));

        if (WwiseVersions.Version > 118)
        {
            IsContinuousValidation = Ar.Read<byte>() is not 0;
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(ChildIds));
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName(nameof(Layers));
        serializer.Serialize(writer, Layers);

        writer.WritePropertyName(nameof(IsContinuousValidation));
        writer.WriteValue(IsContinuousValidation);

        writer.WriteEndObject();
    }
}
