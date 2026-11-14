using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.HairStrands;

[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 8)]
public struct FVector4_16 : IUStruct
{
    public Half X;
    public Half Y;
    public Half Z;
    public Half W;
}
