using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawCoordinateSystem
{
    public ushort XAxis;
    public ushort YAxis;
    public ushort ZAxis;

    public RawCoordinateSystem(FArchiveBigEndian Ar)
    {
        XAxis = Ar.Read<ushort>();
        YAxis = Ar.Read<ushort>();
        ZAxis = Ar.Read<ushort>();
    }
}
