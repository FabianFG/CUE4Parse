using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse_Conversion.V2.Formats.Materials;

public interface IMaterialExportFormat : IExportFormat
{
    public ExportFile Build(string objectName, CMaterialParams2 parameters, string packageDirectory = "");
}

