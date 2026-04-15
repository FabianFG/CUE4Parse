using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports.Rig;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class DnaExporter(UDNAAsset dna) : ExporterBase2(dna)
{
    protected override async Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        var bytes = dna.DNAData?.Value ?? [];
        if (bytes.Length == 0)
        {
            throw new Exception("DNA asset contains no data");
        }

        string? suffix = null;
        if (!string.IsNullOrEmpty(dna.DnaFileName))
        {
            suffix = $"/{Path.GetFileNameWithoutExtension(dna.DnaFileName)}";
        }

        var file = new ExportFile("dna", dna.DNAData?.Value ?? [], suffix);
        var result = await WriteExportFileAsync(file, ct).ConfigureAwait(false);
        return [result];
    }
}
