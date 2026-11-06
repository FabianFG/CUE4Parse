using CUE4Parse.UE4.Assets.Readers;
using FTracepoint = CUE4Parse.UE4.Assets.Exports.Verse.FSolarisPackageDebugData.FFunctionTracepoint;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public class FSolarisClientDebugData : ISolarisDebugData
{
    public string[] SnippetPaths;
    public FFunction[] Functions;
    public FSmallTracepoint[] SmallTracepoints;
    public FTracepoint[] LargeTracepoints;
    public Dictionary<ulong, int> FunctionLookup;
    public Dictionary<string, int> CollisionLookup;

    public FSolarisClientDebugData(FAssetArchive Ar)
    {
        SnippetPaths = Ar.ReadArray(Ar.ReadFUtf8String);
        Functions = Ar.ReadArray<FFunction>();
        SmallTracepoints = Ar.ReadArray<FSmallTracepoint>();
        LargeTracepoints = Ar.ReadArray<FTracepoint>();
        FunctionLookup = Ar.ReadMap(Ar.Read<ulong>, Ar.Read<int>);
        CollisionLookup = Ar.ReadMap(Ar.ReadFString, Ar.Read<int>);
    }

    public struct FFunction
    {
        public int FirstTracepointRef;
        public int NumTracepoints;
    };

    public struct FSmallTracepoint
    {
        public ushort ByteCodeOffset;
        public ushort SnippetIndex;
        public ushort Row;
        public ushort Column;
    };
}
