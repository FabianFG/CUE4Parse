using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyMusicSwitchContainer : BaseHierarchyMusic
{
    public readonly AkMeterInfo MeterInfo;
    public readonly AkStinger[] Stingers;
    public readonly AkMusicTransitionRule MusicTransitionRule;

    public readonly EAkGroupType GroupType;
    public readonly uint GroupId;
    public readonly uint DefaultSwitch;
    public readonly bool IsContinuousValidation;
    public readonly AkMusicSwitchAssoc[] MusicSwitchAssoc = [];

    public readonly byte IsContinuePlayback;
    public readonly AkGameSync[] Arguments = [];
    public readonly EAkDecisionTreeMode Mode;
    public readonly AkDecisionTree DecisionTree;

    // CAkMusicSwitchCntr::SetInitialValues
    public HierarchyMusicSwitchContainer(FWwiseArchive Ar) : base(Ar)
    {
        MeterInfo = new AkMeterInfo(Ar);
        Stingers = AkStinger.ReadArray(Ar);

        MusicTransitionRule = new AkMusicTransitionRule(Ar);

        if (Ar.Version <= 72)
        {
            DecisionTree = new AkDecisionTree(); // Empty tree for old versions
            GroupType = (EAkGroupType) Ar.Read<uint>();
            GroupId = Ar.Read<uint>();
            DefaultSwitch = Ar.Read<uint>();
            IsContinuousValidation = Ar.ReadBool();
            MusicSwitchAssoc = Ar.ReadArray(() => new AkMusicSwitchAssoc(Ar));
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

        if (WwiseConverter.WwiseVersion.Value <= 72)
        {
            writer.WritePropertyName(nameof(GroupType));
            writer.WriteValue(GroupType.ToString());

            writer.WritePropertyName(nameof(GroupId));
            writer.WriteValue(GroupId);

            writer.WritePropertyName(nameof(DefaultSwitch));
            writer.WriteValue(DefaultSwitch);

            writer.WritePropertyName(nameof(IsContinuousValidation));
            writer.WriteValue(IsContinuousValidation);

            writer.WritePropertyName(nameof(MusicSwitchAssoc));
            serializer.Serialize(writer, MusicSwitchAssoc);
        }

        writer.WritePropertyName(nameof(IsContinuePlayback));
        serializer.Serialize(writer, IsContinuePlayback);

        writer.WritePropertyName(nameof(Mode));
        writer.WriteValue(Mode.ToString());

        writer.WritePropertyName(nameof(Arguments));
        serializer.Serialize(writer, Arguments);

        if (WwiseConverter.WwiseVersion.Value > 72)
        {
            writer.WritePropertyName(nameof(DecisionTree));
            serializer.Serialize(writer, DecisionTree);
        }

        writer.WriteEndObject();
    }
}
