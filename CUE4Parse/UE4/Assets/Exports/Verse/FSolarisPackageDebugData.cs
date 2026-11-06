using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public class FSolarisPackageDebugData : ISolarisDebugData
{
    // All snippets used by this debug data
    public FSnippet[] Snippets;
    // All functions of this package and their debug info
    // FunctionIds are indices into this array
    public FFunctionDebugInfo[] Functions;

    public FSolarisPackageDebugData(FAssetArchive Ar)
    {
        Snippets = Ar.ReadArray(() => new FSnippet(Ar));
        Functions = Ar.ReadArray(() => new FFunctionDebugInfo(Ar));
    }

    public class FSnippet(FAssetArchive Ar)
    {
        public string Path = Ar.ReadFUtf8String();
    }

    public class FFunctionDebugInfo
    {
        /** Fully qualified function asset path */
        public string FunctionPathName;
        public FFunctionTracepoint[] Tracepoints;

        public FFunctionDebugInfo(FAssetArchive Ar)
        {
            FunctionPathName = Ar.ReadFString();
            Tracepoints = Ar.ReadArray<FFunctionTracepoint>();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FFunctionTracepoint
    {
        // CodeSkipSizeType
        public uint ByteCodeOffset;
        /** Index into snippet array */
        public int SnippetIndex;
        public STextPosition Locus;

        public FFunctionTracepoint(FAssetArchive Ar)
        {
            ByteCodeOffset = Ar.Read<uint>();
            SnippetIndex = Ar.Read<int>();
            Locus = Ar.Read<STextPosition>();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STextPosition
    {
        public uint Row;
        public uint Column;
    }
}
