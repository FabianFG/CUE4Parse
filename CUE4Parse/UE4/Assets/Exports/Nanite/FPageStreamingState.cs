using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FPageStreamingState
{
    public uint BulkOffset;
    public uint BulkSize;
    public uint PageSize;
    public uint DependenciesStart;
    public uint DependenciesNum;
    public byte MaxHierarchyDepth;
    public NANITE_PAGE_FLAG Flags;

    public FPageStreamingState(FAssetArchive Ar)
    {
        BulkOffset = Ar.Read<uint>();
        BulkSize = Ar.Read<uint>();
        PageSize = Ar.Read<uint>();
        DependenciesStart = Ar.Read<uint>();
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            DependenciesNum = Ar.Read<ushort>();
            MaxHierarchyDepth = Ar.Read<byte>();
            Flags = (NANITE_PAGE_FLAG) Ar.Read<byte>();
        }
        else
        {
            DependenciesNum = Ar.Read<uint>();
            // doesn't exist in 5.0EA
            Flags = (NANITE_PAGE_FLAG) (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES ? Ar.Read<uint>() : 0);
        }
    }
}
