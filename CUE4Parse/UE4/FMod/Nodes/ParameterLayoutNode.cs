using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class ParameterLayoutNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid ParameterGuid;
    public readonly FModGuid LegacyGuid;
    public readonly FModGuid[] Instruments = [];
    public readonly uint Flags;
    public readonly FTriggerBoxParameterLayout[] TriggerBoxes = [];

    public ParameterLayoutNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        ParameterGuid = new FModGuid(Ar);

        if (FModReader.Version < 0x6d)
            LegacyGuid = new FModGuid(Ar);

        if (FModReader.Version >= 0x82)
        {
            Instruments = FModReader.ReadElemListImp<FModGuid>(Ar);
            Flags = Ar.ReadUInt32();
            return;
        }

        if (FModReader.Version >= 0x6a)
        {
            TriggerBoxes = FModReader.ReadElemListImp<FTriggerBoxParameterLayout>(Ar);
            Flags = Ar.ReadUInt32();
            return;
        }
        else
        {
            // Convert legacy trigger boxes to new format
            var legacy = FModReader.ReadElemListImp<FLegacyTriggerBox>(Ar);
            var converted = new FTriggerBoxParameterLayout[legacy.Length];
            for (int i = 0; i < legacy.Length; i++)
            {
                converted[i] = new FTriggerBoxParameterLayout(
                    legacy[i].InstrumentGuid, 0f, 0f, true
                );

            }
            TriggerBoxes = converted;
        }
    }
}
