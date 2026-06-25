using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public class UVerseDebugData : UObject
{
    public FSolarisPackageDebugData? DebugData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.ReadBoolean())
            DebugData = new FSolarisPackageDebugData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(DebugData));
        serializer.Serialize(writer, DebugData);
    }

    public class FSolarisPackageDebugData
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
            public byte[] Data = Ar.ReadArray<byte>();
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
}
