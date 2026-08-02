using System.Collections.Generic;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Formats.Animations;

public interface IAnimExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> BuildAnimation(string objectName, ExportOptions options, CAnimSet animSet);
    public IReadOnlyList<ExportFile> BuildAnimStreamable(string objectName, ExportOptions options, UAnimStreamable animStreamable);
}
