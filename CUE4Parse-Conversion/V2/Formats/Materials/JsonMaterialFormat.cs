using System.Collections.Generic;
using System.Linq;
using System.Text;
using CUE4Parse.UE4.Assets.Exports.Material;
using Newtonsoft.Json;

namespace CUE4Parse_Conversion.V2.Formats.Materials;

public sealed class JsonMaterialFormat : IMaterialExportFormat
{
    public string DisplayName => "JSON";

    public ExportFile Build(string objectName, CMaterialParams2 parameters, string packageDirectory = "")
    {
        var json = JsonConvert.SerializeObject(new MaterialJsonPayload
        {
            Textures = parameters.Textures.ToDictionary(kv => kv.Key, kv => kv.Value.GetPathName()),
            Parameters = parameters
        }, Formatting.Indented);

        return new ExportFile("json", Encoding.UTF8.GetBytes(json));
    }

    private sealed class MaterialJsonPayload
    {
        public Dictionary<string, string>? Textures { get; init; }
        public CMaterialParams2? Parameters { get; init; }
    }
}

