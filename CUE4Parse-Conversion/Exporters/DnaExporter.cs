using CUE4Parse_Conversion.DNA;
using CUE4Parse_Conversion.Formats.PoseAsset;
using CUE4Parse.UE4.Assets.Exports.Rig;

namespace CUE4Parse_Conversion.Exporters;

public sealed class DnaExporter(UDNAObject dna) : ExporterBase(dna)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
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

        var results = new List<ExportFile>
        {
            new("dna", bytes, suffix)
        };

        if (!dna.TryConvert(out var convertedPoseAsset))
            throw new Exception($"Failed to evaluate DNA pose for {ObjectName}");

        ct.ThrowIfCancellationRequested();
        var poseName = string.IsNullOrEmpty(dna.DnaFileName)
            ? ObjectName
            : Path.GetFileNameWithoutExtension(dna.DnaFileName);
        var poseFile = new UEFormatPoseFormat().Build(poseName, ObjectPath, Session.Options, convertedPoseAsset);
        results.Add(poseFile with { NameSuffix = suffix });

        return results;
    }
}
