using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FF7.Objects;

public class FMemoryMappedImageResult : FMemoryImageResult
{
    public void LoadFromArchive(FAssetArchive Ar)
    {
        var frozenSize = Ar.Read<uint>();
        var size = Ar.Read<uint>();
        var idk = Ar.Read<ushort>();
        var flags = Ar.Read<ushort>();
        Ar.Position += 8; // maybe PointerTable.LoadFromArchive(Ar, true);
        FrozenObject = Ar.ReadBytes((int) frozenSize);

        var numVTables = Ar.Read<int>();
        var numScriptNames = Ar.Read<int>();
        var numMinimalNames = Ar.Read<int>();
        VTables = Ar.ReadArray(numVTables, () => new FMemoryImageResult.FMemoryImageVTable(Ar));
        ScriptNames = Ar.ReadArray(numScriptNames, () => new FMemoryImageName(Ar));
        MinimalNames = Ar.ReadArray(numMinimalNames, () => new FMemoryImageName(Ar));
    }
}
