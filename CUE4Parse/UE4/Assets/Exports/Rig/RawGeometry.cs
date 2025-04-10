using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawGeometry : IRawBase
{
    public RawMesh[] Meshes;

    public RawGeometry(FArchiveBigEndian Ar)
    {
        Meshes = Ar.ReadArray(() => new RawMesh(Ar));
    }
}
