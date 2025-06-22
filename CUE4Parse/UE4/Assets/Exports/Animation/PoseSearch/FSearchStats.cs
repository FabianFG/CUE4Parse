using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FSearchStats
{
    public float AverageSpeed;
    public float MaxSpeed;
    public float AverageAcceleration;
    public float MaxAcceleration;
};
