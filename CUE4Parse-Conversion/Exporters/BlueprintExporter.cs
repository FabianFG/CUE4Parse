using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Exporters;

public sealed class BlueprintExporter(UBlueprintGeneratedClass export) : SceneExporter(export)
{
    protected override SceneDto CreateScene(CancellationToken ct) => new BlueprintDto(export, ct);
}
