using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion.Formats.Grooms;

public interface IGroomExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> Build(string objectName, string objectPath, string saveDirectory, ExportOptions options, GroomDto groom, CancellationToken ct);
}
