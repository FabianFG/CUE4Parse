using System.Text;
using CUE4Parse.UE4.Assets.Exports;
using Newtonsoft.Json;

namespace CUE4Parse_Conversion.Exporters;

public class JsonPropertiesExporter(UObject obj) : ExporterBase(obj, "JsonProperties")
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
        return [new ExportFile("json", Encoding.UTF8.GetBytes(json))];
    }
}

// TODO: to json conversion is much more than simple UObjects
// it can be extension based (lua, locmeta, bin, etc)
// it can require a custom step (GAME_Aion2, GAME_RocoKingdomWorld, GAME_AshesOfCreation, etc)
// it's basically anything FModel shows as `SetDocumentText` so all this logic must be migrated and reusable
// same goes for texture, that's a problem for later
