using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto.World;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Formats.World;

public class UEFormatWorldFormat : IWorldExportFormat
{
    public string DisplayName => "UEFormat (ueworld)";

    public ExportFile Build(WorldDto dto, IReadOnlyDictionary<FPackageIndex, string>? meshes = null, IReadOnlyList<string>? subLayers = null, IReadOnlyDictionary<string, string>? worlds = null)
    {
        throw new System.NotImplementedException();
    }
}
