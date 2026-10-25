using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Dto.Custom;

namespace CUE4Parse_Conversion.Exporters.Custom;

public sealed class DeadByDaylightMapExporter(UPaperTileMap map, int seed = 1) : SceneExporter(map)
{
    protected override SceneDto CreateScene(CancellationToken ct) => new DeadByDaylightMapDto(map, seed, ct);
}
