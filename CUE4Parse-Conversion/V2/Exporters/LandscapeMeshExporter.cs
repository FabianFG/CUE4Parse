using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse_Conversion.Landscape;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using SixLabors.ImageSharp.Formats.Png;
using SkiaSharp;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class LandscapeMeshExporter(ALandscapeProxy actor) : MeshExporter<ALandscapeProxy>(actor)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(ALandscapeProxy actor, IMeshExportFormat format)
    {
        const ELandscapeExportFlags flags = ELandscapeExportFlags.All; // TODO: options

        var dto = new LandscapeMesh(actor, flags);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Landscape mesh has no LODs");
        }

        var additional = new List<ExportFile>();
        if (dto.HeightmapTexture is { } heightmap)
        {
            using var stream = new MemoryStream();
            heightmap.Save(stream, new PngEncoder());
            additional.Add(new ExportFile("png", stream.GetBuffer(), "/heightmap"));
        }

        if (dto.NormalTexture is { } normal)
        {
            var imageData = normal.Encode(SKEncodedImageFormat.Png, 100).ToArray();
            additional.Add(new ExportFile("png", imageData, "/NormalMap_DX"));
        }

        if (dto.WeightmapTextures is { } weightmaps)
        {
            foreach (var kv in weightmaps)
            {
                var imageData = kv.Value.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                additional.Add(new ExportFile("png", imageData, $"/{kv.Key}"));
            }
        }

        additional.Add(new ExportFile("", Encoding.UTF8.GetBytes(actor.LandscapeGuid.ToString()), $"/Guid_{actor.LandscapeGuid}"));

        var materialPaths = EnqueueMaterials(dto.Materials);
        return [..format.BuildStaticMesh(ObjectName, Session.Options, dto, materialPaths), ..additional];
    }
}

public sealed class LandscapeMeshExporter2(ULandscapeComponent component) : MeshExporter<ULandscapeComponent>(component)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(ULandscapeComponent component, IMeshExportFormat format)
    {
        var dto = new LandscapeMesh(component);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Landscape mesh has no LODs");
        }

        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, Session.Options, dto, materialPaths);
    }
}
