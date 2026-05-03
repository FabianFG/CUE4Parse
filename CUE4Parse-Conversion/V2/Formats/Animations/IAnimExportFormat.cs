using System.Collections.Generic;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.V2.Options;

namespace CUE4Parse_Conversion.V2.Formats.Animations;

public interface IAnimExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> Build(string objectName, ExportOptions options, CAnimSet animSet);
}

