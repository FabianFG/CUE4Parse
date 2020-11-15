using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum EExportCommandType : uint
    {
        ExportCommandType_Create,
        ExportCommandType_Serialize,
        ExportCommandType_Count
    };
    
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FExportBundleEntry
    {
        public readonly uint LocalExportIndex;
        public readonly EExportCommandType CommandType;
    }
}