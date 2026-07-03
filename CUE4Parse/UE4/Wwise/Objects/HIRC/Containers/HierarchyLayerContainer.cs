using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyLayerContainer : AbstractHierarchy
{
    public readonly BaseHierarchy BaseParams;
    public readonly uint[] ChildIds;
    public readonly CAkLayer[] Layers;
    public readonly bool IsContinuousValidation;

    // CAkBankMgr::StdBankRead<CAkLayerCntr>
    // CAkLayerCntr::SetInitialValues
    public HierarchyLayerContainer(FWwiseArchive Ar) : base()
    {
        Id = Ar.Read<uint>();
        BaseParams = new BaseHierarchy(Ar);
        ChildIds = new AkChildren(Ar).ChildIds;
        Layers = Ar.ReadArray((int) Ar.Read<uint>(), () => new CAkLayer(Ar));

        if (Ar.Version > 118)
        {
            IsContinuousValidation = Ar.ReadBool();
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(BaseParams));
        writer.WriteStartObject();
        BaseParams.WriteJson(writer, serializer);
        writer.WriteEndObject();

        writer.WritePropertyName(nameof(ChildIds));
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName(nameof(Layers));
        serializer.Serialize(writer, Layers);

        writer.WritePropertyName(nameof(IsContinuousValidation));
        writer.WriteValue(IsContinuousValidation);

        writer.WriteEndObject();
    }
}
