using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Exports.Paper2D;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;

internal readonly struct FProceduralTileCell(ETileType type, EPathType path, int number, int flags, int tag = 0, EDensity density = EDensity.Unspecified, int quadrantRotation = 0)
{
    public readonly ETileType Type = type;
    public readonly EDensity Density = density;
    public readonly EPathType Path = path;
    public readonly int Number = number;
    public readonly int Flags = flags;
    public readonly int Tag = tag;
    public readonly int QuadrantRotation = quadrantRotation;

    public int Rotation => Flags switch
    {
        3 => 270,
        5 => 90,
        6 => 180,
        _ => 0
    };

    public FProceduralTileCell(FPaperTileInfo type, FPaperTileInfo path, FPaperTileInfo number, FPaperTileInfo tag = default, FPaperTileInfo density = default, FPaperTileInfo quadrantRotation = default) : this(ETileType.None, EPathType.None, 0, 0)
    {
        if (type.IsValid())
        {
            Type = (ETileType) (type.GetTileIndex() + 1);
            Flags = type.GetFlagsAsIndex();
        }

        if (path.IsValid())
        {
            Path = (EPathType) (path.GetTileIndex() + 1);
            Flags = path.GetFlagsAsIndex();
        }

        if (number.IsValid()) Number = number.GetTileIndex() + 1;
        if (tag.IsValid()) Tag = tag.GetTileIndex() + 1;
        if (density.IsValid()) Density = (EDensity) (density.GetTileIndex() + 1);
        if (quadrantRotation.IsValid()) QuadrantRotation = quadrantRotation.GetTileIndex() + 1;
    }
}
