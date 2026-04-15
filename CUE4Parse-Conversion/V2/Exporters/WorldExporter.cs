using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class WorldExporter(UWorld world) : ExporterBase2(world)
{
    protected override Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("Exporting worlds is not yet implemented.");
    }
}
