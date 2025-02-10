using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FBulkDataMapEntry
    {
        public const uint Size = 32;

        public readonly ulong SerialOffset;
        public readonly ulong DuplicateSerialOffset;
        public readonly ulong SerialSize;
        public readonly uint Flags;
        public readonly FBulkDataCookedIndex CookedIndex; // https://github.com/EpicGames/UnrealEngine/commit/6e7f2558611221cfdf413106900caf947e3c17c5
        public readonly short _pad0;
        public readonly byte _pad1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FBulkDataCookedIndex
    {
        public static FBulkDataCookedIndex Default => new(0);

        public readonly byte Value;

        public bool IsDefault => Value == 0;

        public FBulkDataCookedIndex(byte value)
        {
            Value = value;
        }

        public string GetAsExtension() => IsDefault ? string.Empty : Value.ToString("D3");

        public override string ToString() => GetAsExtension();
    }
}
