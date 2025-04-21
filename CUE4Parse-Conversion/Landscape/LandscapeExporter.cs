using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SkiaSharp;

namespace CUE4Parse_Conversion.Landscape;

[Flags]
public enum ELandscapeExportFlags
{
    Heightmap = 1 << 0,
    Weightmap = 1 << 1,
    Mesh = 1 << 3,
    All = Heightmap | Weightmap | Mesh
}

public class LandscapeExporter : ExporterBase
{
    private readonly ELandscapeExportFlags _flags;
    private FGuid LandscapeGuid { get; }

    internal Mesh[]? _processedFiles;

    public LandscapeExporter(ALandscapeProxy landscape, ULandscapeComponent[] components, ExporterOptions options, ELandscapeExportFlags flags = ELandscapeExportFlags.All) : base(landscape, options)
    {
        _flags = flags;
        LandscapeGuid = landscape.LandscapeGuid;

        if(landscape.TryConvert(components, flags, out var lod, out var heightMaps,  out var weightMaps))
            SetMeshData(lod, heightMaps, weightMaps);
    }

    private void SetMeshData(CStaticMesh mesh, Dictionary<string, Image> heightMaps, Dictionary<string, SKBitmap> weightMaps)
    {
        var final = new List<Mesh>();
        var path = GetExportSavePath();

        if (_flags.HasFlag(ELandscapeExportFlags.Mesh))
        {
            Debug.Assert(mesh.LODs.First() != null, nameof(mesh.LODs) + " != null");
            using var Ar = new FArchiveWriter();
            var materialExports = Options.ExportMaterials ? new List<MaterialExporter2>() : null;
            string ext;
            switch (Options.MeshFormat)
            {
                case EMeshFormat.ActorX:
                    ext = "pskx";
                    new ActorXMesh(mesh.LODs.First(), materialExports, Array.Empty<FPackageIndex>(), Options).Save(Ar);
                    break;
                case EMeshFormat.Gltf2:
                    ext = "glb";
                    new Gltf(ExportName, mesh.LODs.First(), materialExports, Options).Save(Options.MeshFormat, Ar);
                    break;
                case EMeshFormat.OBJ:
                    ext = "obj";
                    new Gltf(ExportName, mesh.LODs.First(), materialExports, Options).Save(Options.MeshFormat, Ar);
                    break;
                case EMeshFormat.UEFormat: {
                    ext = "uemodel";
                    new UEModel(ExportName, mesh, new FPackageIndex(), Options).Save(Ar);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
            }

            final.Add(new Mesh($"{path}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
        }

        if (_flags.HasFlag(ELandscapeExportFlags.Weightmap))
        {
            foreach (var kv in weightMaps)
            {
                string weightMapPath = $"{path}/{kv.Key}.png";
                var weightMap = kv.Value;
                var imageData = weightMap.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                final.Add(new Mesh(weightMapPath, imageData, new List<MaterialExporter2>()));
            }
        }

        if (_flags.HasFlag(ELandscapeExportFlags.Heightmap))
        {
            foreach (var kv in heightMaps)
            {
                string heightMapPath = $"{path}/{kv.Key}.png";
                var stream = new MemoryStream();
                kv.Value.Save(stream, new PngEncoder());
                final.Add(new Mesh(heightMapPath, stream.GetBuffer(), new List<MaterialExporter2>()));
                stream.Dispose();
            }
        }

        final.Add(new Mesh($"{path}/Guid_{LandscapeGuid}", Encoding.UTF8.GetBytes(LandscapeGuid.ToString()), []));

        _processedFiles = final.ToArray();
        weightMaps.Clear();
        final.Clear();
    }

    public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
    {
        Debug.Assert(_processedFiles != null, nameof(_processedFiles) + " != null");
        var b = false;
        label = string.Empty;
        savedFilePath = string.Empty;
        foreach (var pf in _processedFiles.Reverse())
        { // hack to get the label from first one
            b |= pf.TryWriteToDir(baseDirectory, out label, out savedFilePath);
        }

        return b; // savedFilePath != string.Empty && File.Exists(savedFilePath);
    }

    public override bool TryWriteToZip(out byte[] zipFile)
    {
        throw new NotImplementedException();
    }

    public override void AppendToZip()
    {
        throw new NotImplementedException();
    }
}
