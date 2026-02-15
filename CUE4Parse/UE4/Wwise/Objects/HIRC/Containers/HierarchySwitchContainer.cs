using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkSwitchCntr
public class HierarchySwitchContainer : BaseHierarchy
{
    public readonly byte GroupType;
    public readonly uint GroupId;
    public readonly uint DefaultSwitch;
    public readonly bool IsContinuousValidation;
    public readonly uint[] ChildIds;
    public readonly AkSwitchPackage[] SwitchPackages;
    public readonly AkSwitchParams[] SwitchParams;

    // CAkSwitchCntr::SetInitialValues
    public HierarchySwitchContainer(FArchive Ar) : base(Ar)
    {
        GroupType = Ar.Read<byte>();
        GroupId = Ar.Read<uint>();
        DefaultSwitch = Ar.Read<uint>();
        IsContinuousValidation = Ar.Read<byte>() is not 0;
        ChildIds = new AkChildren(Ar).ChildIds;
        SwitchPackages = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkSwitchPackage(Ar));
        SwitchParams = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkSwitchParams(Ar));
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(GroupType));
        writer.WriteValue(GroupType);

        writer.WritePropertyName(nameof(GroupId));
        writer.WriteValue(GroupId);

        writer.WritePropertyName(nameof(DefaultSwitch));
        writer.WriteValue(DefaultSwitch);

        writer.WritePropertyName(nameof(IsContinuousValidation));
        writer.WriteValue(IsContinuousValidation);

        writer.WritePropertyName(nameof(ChildIds));
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName(nameof(SwitchPackages));
        serializer.Serialize(writer, SwitchPackages);

        writer.WritePropertyName(nameof(SwitchParams));
        serializer.Serialize(writer, SwitchParams);

        writer.WriteEndObject();
    }
}
