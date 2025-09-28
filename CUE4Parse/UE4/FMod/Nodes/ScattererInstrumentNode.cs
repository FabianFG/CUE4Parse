using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class ScattererInstrumentNode
{
    public readonly FModGuid BaseGuid;
    public readonly int MaximumSpawnPolyphony;
    public readonly int SpawnCount;
    public readonly FRangeFloat SpawnTime;
    public readonly int SpawnPolyphonyLimitBehavior;
    public readonly float SpawnRate;
    public readonly FQuantization? SpawnQuantization;

    public ScattererInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        MaximumSpawnPolyphony = Ar.ReadInt32();
        SpawnCount = Ar.ReadInt32();

        if (FModReader.Version >= 0x8a)
        {
            if (FModReader.Version < 0x8e && SpawnCount == int.MaxValue)
            {
                SpawnCount = 0x21;
            }
        }
        else if (SpawnCount == 0 || SpawnCount == int.MaxValue)
        {
            SpawnCount = 0x21;
        }

        SpawnTime = new FRangeFloat(Ar);

        if (FModReader.Version >= 0x39)
        {
            SpawnPolyphonyLimitBehavior = Ar.ReadInt32();
        }

        if (FModReader.Version >= 0x5e)
        {
            SpawnRate = Ar.ReadSingle();
        }

        if (FModReader.Version >= 0x85)
        {
            SpawnQuantization = new FQuantization(Ar);
        }
    }
}
