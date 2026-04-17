using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse_Conversion.Landscape;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Actor;
using SixLabors.ImageSharp.Formats.Png;
using SkiaSharp;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class LandscapeMeshExporter(ALandscapeProxy actor) : MeshExporter2<ALandscapeProxy>(actor)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(ALandscapeProxy actor, IMeshExportFormat format)
    {
        const ELandscapeExportFlags flags = ELandscapeExportFlags.All; // TODO: options

        if (!actor.TryConvert(null, flags, out var convertedMesh, out var heightmaps, out var weightmaps) || convertedMesh.LODs.Count == 0)
        {
            throw new Exception("Failed to convert landscape mesh or no LODs");
        }

        if (Session.Options.ExportMaterials)
        {
            EnqueueMaterials(actor.LandscapeMaterial.ResolvedObject);
        }

        var additional = new List<ExportFile>();
        if (flags.HasFlag(ELandscapeExportFlags.Heightmap))
        {
            var encoder = new PngEncoder();
            using var stream = new MemoryStream();
            foreach (var kv in heightmaps)
            {
                kv.Value.Save(stream, encoder);
                additional.Add(new ExportFile("png", stream.GetBuffer(), $"/{kv.Key}"));
                stream.SetLength(0);
            }
        }

        if (flags.HasFlag(ELandscapeExportFlags.Weightmap))
        {
            foreach (var kv in weightmaps)
            {
                var imageData = kv.Value.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                additional.Add(new ExportFile("png", imageData, $"/{kv.Key}"));
            }
        }

        additional.Add(new ExportFile("", Encoding.UTF8.GetBytes(actor.LandscapeGuid.ToString()), $"/Guid_{actor.LandscapeGuid}"));

        return [..format.BuildStaticMesh(ObjectName, Session.Options, convertedMesh), ..additional];
    }
}
