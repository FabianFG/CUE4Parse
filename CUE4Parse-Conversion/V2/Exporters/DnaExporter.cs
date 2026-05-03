using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Rig;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class DnaExporter(UDNAAsset dna) : ExporterBase(dna)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles()
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

        return [new ExportFile("dna", dna.DNAData?.Value ?? [], suffix)];
    }
}
