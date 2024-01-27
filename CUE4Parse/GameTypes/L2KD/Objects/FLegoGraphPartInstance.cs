using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using System.Runtime.InteropServices;

namespace CUE4Parse.GameTypes.L2KD.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FLegoGraphPartInstance : IUStruct
    {
        public readonly uint Id;
        public readonly uint Color;

        public FLegoGraphPartInstance(FAssetArchive Ar) {
            Id = Ar.Read<uint>();
            Color = Ar.Read<uint>();
        }

        public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(Color)}: {Color}";
    }
}