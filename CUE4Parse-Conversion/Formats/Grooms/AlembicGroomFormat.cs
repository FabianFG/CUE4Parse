using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.Alembic;

namespace CUE4Parse_Conversion.Formats.Grooms;

public sealed class AlembicGroomFormat : IGroomExportFormat
{
    public string DisplayName => "Alembic (abc)";
    public string Extension => "abc";

    public IReadOnlyList<ExportFile> Build(string objectName, string objectPath, string saveDirectory, ExportOptions options, GroomDto groom, CancellationToken ct)
    {
        if (!groom.Groups.Any(group => group.Strands is not null || group.Guides is not null))
            throw new InvalidDataException("The groom has no cooked strands or guides.");

        return [new ExportFile(Extension, AlembicGroom.Write(groom, objectName, ct))];
    }
}
