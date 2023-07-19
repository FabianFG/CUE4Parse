using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.i18N
{
    public class FTextLocalizationResourceString : IUStruct
    {
        public readonly string String;
        public int RefCount;
        
        public FTextLocalizationResourceString(FArchive Ar)
        {
            String = Ar.ReadFString();
            RefCount = Ar.Read<int>();
        }
        
        public FTextLocalizationResourceString(string s, int refCount)
        {
            String = s;
            RefCount = refCount;
        }
    }
}