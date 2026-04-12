using System.Collections.Generic;
using CUE4Parse_Conversion.Animations.PSA;

namespace CUE4Parse_Conversion.V2.Formats.Animations;

public interface IAnimExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> Build(string objectName, ExporterOptions options, CAnimSet animSet);
}

