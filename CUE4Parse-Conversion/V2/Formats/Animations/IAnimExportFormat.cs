using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Options;
using CUE4Parse_Conversion.V2.Writers.ActorX.Structs.Animations;

namespace CUE4Parse_Conversion.V2.Formats.Animations;

public interface IAnimExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> Build(string objectName, ExportOptions options, CAnimSet animSet);
}

