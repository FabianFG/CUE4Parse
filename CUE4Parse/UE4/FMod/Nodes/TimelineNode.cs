using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class TimelineNode
{
    public readonly FModGuid BaseGuid;
    public readonly FTriggerBox[] TriggerBoxes = [];
    public readonly FTriggerBox[] TimeLockedTriggerBoxes = [];
    //public uint[] LegacyUIntArray { get; } = [];

    public TimelineNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);

        if (FModReader.Version >= 0x6d)
        {
            TriggerBoxes = FModReader.ReadElemListImp<FTriggerBox>(Ar);
            TimeLockedTriggerBoxes = FModReader.ReadElemListImp<FTriggerBox>(Ar);
        }

        if (FModReader.Version < 0x84)
        {
            // LegacyUIntArray, I don't care
        }
    }
}
