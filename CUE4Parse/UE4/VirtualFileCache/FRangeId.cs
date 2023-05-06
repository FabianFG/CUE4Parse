using System.IO;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.VirtualFileCache
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRangeId
    {
        public readonly int FileId;
        public readonly FBlockRange Range;

        public string GetFileName() => $"vfc_{FileId}.data";
        public string GetPersistentDownloadPath() => Path.Combine("VFC", GetFileName());

        public override string ToString() => $"{GetFileName()}: {Range}";
    }
}
