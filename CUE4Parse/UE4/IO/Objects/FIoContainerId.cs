using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIoContainerId
    {
        public readonly ulong Id;

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}