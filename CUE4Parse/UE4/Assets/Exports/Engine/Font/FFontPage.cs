using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FFontPage : IUStruct
    {
        public FFontPage(FAssetArchive Ar)
        {
            Ar.ReadArray(() => new FPackageIndex(Ar)); // Textures
            Ar.ReadArray(() => new FFontCharacter(Ar)); // Characters
        }
    }
}
