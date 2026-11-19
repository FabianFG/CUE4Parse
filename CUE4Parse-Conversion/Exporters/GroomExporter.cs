using CUE4Parse.UE4.Assets.Exports.HairStrands;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Grooms;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion.Exporters;

public sealed class GroomExporter(UGroomAsset groom) : ExporterBase(groom)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        var format = GetGroomFormat(Session.Options.MeshFormat);
        return format.Build(ObjectName, ObjectPath, SaveDirectory, Session.Options, new GroomDto(groom, ct), ct);
    }

#pragma warning disable CA1859
    private static IGroomExportFormat GetGroomFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.Alembic => new AlembicGroomFormat(),
        // EMeshFormat.UEFormat => new UEFormatGroomFormat(),
        _ => throw new NotSupportedException($"Groom export does not support format {format}. Available formats: {string.Join(", ", "Alembic")}")
    };
#pragma warning restore CA1859
}
