using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Objects.Core.Serialization
{
    /// <summary>
    /// Structure to hold unique custom key with its version.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FCustomVersion
    {
        /** Unique custom key. */
        public FGuid Key;
        /** Custom version */
        public int Version;

        public override string ToString() => $"{nameof(Key)}: {Key}, {nameof(Version)}: {Version}";
    }
}