using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchySwitchContainer : BaseHierarchy
{
    public byte GroupType { get; private set; }
    public uint GroupId { get; private set; }
    public uint DefaultSwitch { get; private set; }
    public byte IsContinuousValidation { get; private set; }
    public uint[] ChildIds { get; private set; }

    public List<AkSwitchPackage> SwitchPackages { get; private set; }
    public List<AkSwitchParams> SwitchParams { get; private set; }

    public HierarchySwitchContainer(FArchive Ar) : base(Ar)
    {
        GroupType = Ar.Read<byte>();
        GroupId = Ar.Read<uint>();
        DefaultSwitch = Ar.Read<uint>();
        IsContinuousValidation = Ar.Read<byte>();
        ChildIds = new AkChildren(Ar).ChildIds;

        var numSwitchGroups = Ar.Read<uint>();
        SwitchPackages = [];
        for (var i = 0; i < numSwitchGroups; i++)
        {
            var switchGroup = new AkSwitchPackage(Ar);
            SwitchPackages.Add(switchGroup);
        }

        var numSwitchParams = Ar.Read<uint>();
        SwitchParams = [];
        for (var i = 0; i < numSwitchParams; i++)
        {
            var switchParam = new AkSwitchParams(Ar);
            SwitchParams.Add(switchParam);
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("GroupType");
        writer.WriteValue(GroupType);

        writer.WritePropertyName("GroupId");
        writer.WriteValue(GroupId);

        writer.WritePropertyName("DefaultSwitch");
        writer.WriteValue(DefaultSwitch);

        writer.WritePropertyName("IsContinuousValidation");
        writer.WriteValue(IsContinuousValidation != 0);

        writer.WritePropertyName("ChildIds");
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName("SwitchPackages");
        serializer.Serialize(writer, SwitchPackages);

        writer.WritePropertyName("SwitchParams");
        serializer.Serialize(writer, SwitchParams);

        writer.WriteEndObject();
    }
}
