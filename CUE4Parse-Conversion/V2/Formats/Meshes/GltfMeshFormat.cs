using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public sealed class GltfMeshFormat(bool isObj = false) : IMeshExportFormat
{
    public string DisplayName => isObj ? "Wavefront OBJ" : "glTF 2.0 (binary)";

    private readonly EMeshFormat _legacyFormat = isObj ? EMeshFormat.OBJ : EMeshFormat.Gltf2;
    private readonly string _extension = isObj ? "obj" : "glb";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, SkeletalMesh dto)
    {
        using var ar = new FArchiveWriter();
        new Gltf(objectName, dto, options).Save(_legacyFormat, ar);

        return [new ExportFile(_extension, ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, StaticMesh dto)
    {
        using var ar = new FArchiveWriter();
        new Gltf(objectName, dto, options).Save(_legacyFormat, ar);

        return [new ExportFile(_extension, ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, Skeleton dto)
        => throw new NotSupportedException(
            "glTF does not support skeleton-only exports. Please export a skeletal mesh to get a glTF file containing the skeleton.");
}

