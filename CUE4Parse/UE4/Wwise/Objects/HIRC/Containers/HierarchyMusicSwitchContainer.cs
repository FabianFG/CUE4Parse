using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyMusicSwitchContainer : BaseHierarchyMusic
{
    public readonly AkMeterInfo MeterInfo;
    public readonly AkStinger[] Stingers;
    public readonly AkMusicTransitionRule MusicTransitionRule;
    public readonly byte IsContinuePlayback;
    public readonly AkGameSync[] Arguments = [];
    public readonly EAkDecisionTreeMode Mode;
    public readonly AkDecisionTree DecisionTree;

    // CAkMusicSwitchCntr::SetInitialValues
    public HierarchyMusicSwitchContainer(FArchive Ar) : base(Ar)
    {
        MeterInfo = new AkMeterInfo(Ar);
        Stingers = AkStinger.ReadArray(Ar);

        MusicTransitionRule = new AkMusicTransitionRule(Ar);

        if (WwiseVersions.Version <= 72)
        {
            DecisionTree = new AkDecisionTree(); // Empty tree for old versions
        }
        else
        {
            IsContinuePlayback = Ar.Read<byte>();

            var treeDepth = Ar.Read<uint>();
            Arguments = AkGameSync.ReadSequential(Ar, treeDepth);

            var treeDataSize = Ar.Read<uint>();
            Mode = Ar.Read<EAkDecisionTreeMode>();

            DecisionTree = new AkDecisionTree(Ar, treeDepth, treeDataSize);
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(MeterInfo));
        serializer.Serialize(writer, MeterInfo);

        writer.WritePropertyName(nameof(Stingers));
        serializer.Serialize(writer, Stingers);

        writer.WritePropertyName(nameof(MusicTransitionRule));
        serializer.Serialize(writer, MusicTransitionRule.Rules);

        writer.WritePropertyName(nameof(IsContinuePlayback));
        serializer.Serialize(writer, IsContinuePlayback);

        writer.WritePropertyName(nameof(Mode));
        writer.WriteValue(Mode.ToString());

        writer.WritePropertyName(nameof(Arguments));
        serializer.Serialize(writer, Arguments);

        writer.WritePropertyName(nameof(DecisionTree));
        serializer.Serialize(writer, DecisionTree);

        writer.WriteEndObject();
    }
}
