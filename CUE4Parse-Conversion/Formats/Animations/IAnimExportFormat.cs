using System.Collections.Generic;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;

namespace CUE4Parse_Conversion.Formats.Animations;

public interface IAnimExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> Build(string objectName, ExportOptions options, CAnimSet animSet);
}

