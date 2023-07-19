using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.i18N
{
    public class FEntry : IUStruct
    {
        public string LocalizedString;
        public readonly string LocResName;
        public uint SourceStringHash;
        public readonly int Priority;
        
        public FEntry(FArchive Ar)
        {
            LocalizedString = string.Empty;
            LocResName = Ar.Name;
            SourceStringHash = 0;
            Priority = 0;
        }
    }
}