using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawRBFPose
{
    public string Name;
    public float Scale;

    public RawRBFPose(FArchiveBigEndian Ar)
    {
        Name = Ar.ReadString();
        Scale = Ar.Read<float>();
    }
}
