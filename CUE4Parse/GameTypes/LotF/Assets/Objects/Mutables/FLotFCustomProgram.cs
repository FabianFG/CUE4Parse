using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.LotF.Assets.Objects.Mutables;

public class FLotFCustomProgram
{
    public (ulong Hash, long Size)[] Roms = [];
    public ulong[] TextureRoms = [];
    public (int romIndex, int lodIndex)[] Lods = [];
    public (int romIndex, int opAddress)[][] Unknown = [];

    public FLotFCustomProgram(FMutableArchive Ar)
    {
        Roms = Ar.ReadArray(() => (Ar.Read<ulong>(), Ar.Read<long>()));
        TextureRoms = Ar.ReadArray<ulong>();
        Ar.Position += 4; // 0
        Lods = Ar.ReadArray(() => (Ar.Read<int>(), Ar.Read<int>()));
    }
}
