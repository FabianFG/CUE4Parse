using System.Collections;
using System.Runtime.InteropServices;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.SG2.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FCore1047ReleaseFlag : IUStruct
    {
        public readonly FName Release;

        public FCore1047ReleaseFlag(FArchive Ar)
        {
            Release = Ar.ReadFName();
        }
    }
}
