using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BankHeader
    {
        public readonly uint Version;
        public readonly uint Id;
    }
}
