using System.Text;
using CUE4Parse.UE4.Assets.Exports.Material;
using Newtonsoft.Json;

namespace CUE4Parse_Conversion.Formats.Materials;

public sealed class JsonMaterialFormat : IMaterialExportFormat
{
    public string DisplayName => "JSON";

    public ExportFile Build(string objectName, CMaterialParams2 parameters, string packageDirectory = "")
    {
        var json = JsonConvert.SerializeObject(parameters, Formatting.Indented);
        return new ExportFile("json", Encoding.UTF8.GetBytes(json));
    }
}
