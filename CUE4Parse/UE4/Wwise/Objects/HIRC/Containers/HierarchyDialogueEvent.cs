using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkBankMgr::StdBankRead<CAkDialogueEvent>
public class HierarchyDialogueEvent : AbstractHierarchy
{
    public readonly byte Probability;
    public readonly AkGameSync[] Arguments;
    public readonly EAkDecisionTreeMode Mode;
    public readonly AkDecisionTree DecisionTree;
    public readonly AkPropBundle PropBundle;

    // CAkDialogueEvent::SetInitialValues
    public HierarchyDialogueEvent(FWwiseArchive Ar) : base(Ar)
    {
        if (Ar.Version > 72)
            Probability = Ar.Read<byte>();

        var treeDepth = Ar.Read<uint>();
        Arguments = AkGameSync.ReadSequential(Ar, treeDepth);

        var treeSize = Ar.Read<uint>();

        if (Ar.Version > 45 && Ar.Version <= 72)
            Probability = Ar.Read<byte>();

        if (Ar.Version > 45)
            Mode = Ar.Read<EAkDecisionTreeMode>();

        DecisionTree = new AkDecisionTree(Ar, treeDepth, treeSize);
        PropBundle = new AkPropBundle(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(Probability));
        writer.WriteValue(Probability);

        writer.WritePropertyName(nameof(Arguments));
        serializer.Serialize(writer, Arguments);

        writer.WritePropertyName(nameof(Mode));
        writer.WriteValue(Mode.ToString());

        writer.WritePropertyName(nameof(DecisionTree));
        serializer.Serialize(writer, DecisionTree);

        writer.WritePropertyName(nameof(PropBundle));
        serializer.Serialize(writer, PropBundle);

        writer.WriteEndObject();
    }
}
