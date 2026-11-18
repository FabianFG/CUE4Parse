using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse_Conversion.Writers.UEFormat;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Formats.Animations;

public sealed class UEFormatAnimFormat : IAnimExportFormat
{
    public string DisplayName => "UEFormat (ueanim)";
    public string Extension => "ueanim";

    public IReadOnlyList<ExportFile> Build(string objectName, string objectPath, ExportOptions options, CAnimSet animSet)
    {
        var results = new List<ExportFile>(animSet.Sequences.Count);
        for (var i = 0; i < results.Capacity; i++)
        {
            using var Ar = new FArchiveWriter();
            new UEAnim(objectName, objectPath, animSet, i, options).Save(Ar);

            var suffix = i == 0 ? "" : $"_SEQ{i}";
            results.Add(new ExportFile(Extension, Ar.GetBuffer(), suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildAnimStreamable(string objectName, string objectPath, ExportOptions options, UAnimStreamable animStreamable)
    {
        using var Ar = new FArchiveWriter();
        new UEAnim(objectName, objectPath, animStreamable, options).Save(Ar);
        return [new ExportFile(Extension, Ar.GetBuffer())];
    }
}
