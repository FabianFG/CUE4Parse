using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Objects.UObject
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FUniqueObjectGuid
    {
        public FGuid Guid;
    }
}