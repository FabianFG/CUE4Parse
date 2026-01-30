using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyMusicSwitchContainer : BaseHierarchyMusic
{
    public readonly AkMeterInfo MeterInfo;
    public readonly AkStinger[] Stingers;
    public readonly AkMusicTransitionRule MusicTransitionRule;
    public readonly byte IsContinuePlayback;
    public readonly AkGameSync[] Arguments;
    public readonly byte Mode;
    public readonly AkDecisionTree DecisionTree;

    public HierarchyMusicSwitchContainer(FArchive Ar) : base(Ar)
    {
        MeterInfo = new AkMeterInfo(Ar);
        Stingers = AkStinger.ReadArray(Ar);

        MusicTransitionRule = new AkMusicTransitionRule(Ar);

        Arguments = [];
        if (WwiseVersions.Version <= 72)
        {
            // TODO: GroupSettings = new AkGroupSettings(Ar);
            DecisionTree = new AkDecisionTree(); // Empty tree for old versions
        }
        else
        {
            IsContinuePlayback = Ar.Read<byte>();
            var treeDepth = Ar.Read<uint>();

            Arguments = AkGameSync.ReadSequential(Ar, treeDepth);

            var treeDataSize = Ar.Read<uint>();
            Mode = Ar.Read<byte>();

            DecisionTree = new AkDecisionTree(Ar, treeDepth, treeDataSize);
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("MeterInfo");
        serializer.Serialize(writer, MeterInfo);

        writer.WritePropertyName("Stingers");
        serializer.Serialize(writer, Stingers);

        writer.WritePropertyName("MusicTransitionRule");
        serializer.Serialize(writer, MusicTransitionRule.Rules);

        if (WwiseVersions.Version <= 72)
        {
            //writer.WritePropertyName("GroupSettings");
            //serializer.Serialize(writer, GroupSettings);
        }
        else
        {
            writer.WritePropertyName("IsContinuePlayback");
            serializer.Serialize(writer, IsContinuePlayback);

            writer.WritePropertyName("Mode");
            serializer.Serialize(writer, Mode);

            writer.WritePropertyName("Arguments");
            serializer.Serialize(writer, Arguments);

            writer.WritePropertyName("DecisionTree");
            serializer.Serialize(writer, DecisionTree);
        }

        writer.WriteEndObject();
    }
}
