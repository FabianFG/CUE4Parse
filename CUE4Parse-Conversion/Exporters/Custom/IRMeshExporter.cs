using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;

namespace CUE4Parse_Conversion.Exporters.Custom;

public sealed class IRMeshExporter(UIRMesh originalMesh) : MeshExporter<UIRMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UIRMesh originalMesh, IMeshExportFormat format)
    {
        var result = new List<ExportFile>();
        for (int i = 0; i < originalMesh.Info.PartCount; i++)
        {
            try
            {
                var part = new StaticMeshDto(originalMesh, i);
                var exportFiles = format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, part, EnqueueMaterials(part.Materials));
                result.AddRange(exportFiles.Select(exportFile => exportFile with { NameSuffix = $"_{i}" }));
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to convert IR static mesh part {0}", i);
            }
        }

        return result;
    }
}
