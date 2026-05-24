using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Writers;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromLandscapeMesh(StaticMeshDto owner, ULandscapeComponent[] components, int sizeQuads, ELandscapeExportFlags flags, out ConcurrentDictionary<string, SKBitmap>? bitmaps, out Image<L16>? heightmap)
    {
        var componentSizeQuads = ((sizeQuads + 1) >> 0 /*Landscape->ExportLOD*/) - 1;
        var scale = (float)componentSizeQuads / sizeQuads;
        var componentVertexCount = (componentSizeQuads + 1) * (componentSizeQuads + 1);

        // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4657
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        var numFaces = components.Length * (componentSizeQuads * componentSizeQuads) * 2;
        var indices = new List<uint>(numFaces * 3);
        for (var i = 0; i < components.Length; i++)
        {
            var baseVertIndex = i * componentVertexCount;
            for (var y = 0; y < componentSizeQuads; y++)
            for (var x = 0; x < componentSizeQuads; x++)
            {
                if (true) // (VisibilityData[BaseVertIndex + Y * (ComponentSizeQuads + 1) + X] < VisThreshold)
                {
                    var w1 = baseVertIndex + (x + 0) + (y + 0) * (componentSizeQuads + 1);
                    var w2 = baseVertIndex + (x + 1) + (y + 1) * (componentSizeQuads + 1);
                    var w3 = baseVertIndex + (x + 1) + (y + 0) * (componentSizeQuads + 1);

                    indices.Add((uint)w1);
                    indices.Add((uint)w2);
                    indices.Add((uint)w3);

                    var w4 = baseVertIndex + (x + 0) + (y + 0) * (componentSizeQuads + 1);
                    var w5 = baseVertIndex + (x + 0) + (y + 1) * (componentSizeQuads + 1);
                    var w6 = baseVertIndex + (x + 1) + (y + 1) * (componentSizeQuads + 1);

                    indices.Add((uint)w4);
                    indices.Add((uint)w5);
                    indices.Add((uint)w6);
                }
            }

            components[i].GetComponentExtent(ref minX, ref minY, ref maxX, ref maxY);
        }

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var uvScale = FMeshUVFloat.OneVector / new FMeshUVFloat(width, height);

        var extraUvs = new FMeshUVFloat[1][];
        var vertices = new MeshVertex[components.Length * componentVertexCount];
        var weightmapColors = new ConcurrentDictionary<string, FColor[]>();

        for (var i = 0; i < extraUvs.Length; i++)
        {
            extraUvs[i] = new FMeshUVFloat[vertices.Length];
        }

        var heightmapTexture = flags.HasFlag(ELandscapeExportFlags.Heightmap) ? new Image<L16>(width, height) : null;
        var bitmapTextures = flags.HasFlag(ELandscapeExportFlags.Weightmap) ? new ConcurrentDictionary<string, SKBitmap>() : null;

        for (var i = 0; i < components.Length; i++)
        {
            var cdi = new FLandscapeComponentDataInterface(components[i], 0);
            cdi.EnsureWeightmapTextureDataCache();

            var weightMapAllocs = cdi.Component.GetWeightmapLayerAllocations();
            var compTransform = cdi.Component.GetComponentTransform();
            var relLoc = cdi.Component.GetRelativeLocation();

            var baseVertIndex = i * componentVertexCount;

            Parallel.For(0, componentVertexCount, vertIndex =>
            {
                cdi.VertexIndexToXY(vertIndex, out var vertX, out var vertY);

                var textureUv = new FMeshUVFloat(vertX * scale + cdi.Component.SectionBaseX, vertY * scale + cdi.Component.SectionBaseY);
                var textureUv2 = new TIntVector2<int>((int)textureUv.U - minX, (int)textureUv.V - minY);

                heightmapTexture?.ProcessPixelRows(accessor =>
                {
                    var pixelRow = accessor.GetRowSpan(textureUv2.Y);
                    pixelRow[textureUv2.X] = new L16((ushort)(cdi.GetVertex(vertX, vertY) + relLoc).Z);
                });

                foreach (var allocationInfo in weightMapAllocs)
                {
                    var weight = cdi.GetLayerWeight(vertX, vertY, allocationInfo);
                    if (weight == 0) continue;

                    var layerName = allocationInfo.GetLayerName();

                    var layerColors = weightmapColors.GetOrAdd(layerName, _ => new FColor[vertices.Length]);
                    layerColors[baseVertIndex + vertIndex] = new FColor(weight, weight, weight, 255);

                    if (bitmapTextures != null)
                    {
                        var bitmap = bitmapTextures.GetOrAdd(layerName, _ => new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Unpremul));
                        unsafe
                        {
                            var pixels = (byte*)bitmap.GetPixels();
                            pixels[textureUv2.Y * width + textureUv2.X] = weight;
                        }
                    }
                }

                var position = cdi.GetLocalVertex(vertX, vertY) + relLoc;
                cdi.GetLocalTangentVectors(vertIndex, out var tangentX, out _, out var normal);

                normal /= compTransform.Scale3D;
                normal.Normalize();
                if (normal.ContainsNaN())
                {
                    normal = FVector.UpVector;
                }

                ref var tangent = ref FVector4.AsFVector(ref tangentX);
                tangent /= compTransform.Scale3D;
                tangent.Normalize();
                if (tangent.ContainsNaN())
                {
                    tangent = FVector.RightVector;
                }

                vertices[baseVertIndex + vertIndex] = new MeshVertex(position, normal, tangentX, textureUv);
                extraUvs[0][baseVertIndex + vertIndex] = (textureUv - new FMeshUVFloat(minX, minY)) * uvScale;

                if (bitmapTextures != null)
                {
                    var bitmap = bitmapTextures.GetOrAdd("NormalMap_DX", _ => new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
                    unsafe
                    {
                        var pixels = (byte*)bitmap.GetPixels();
                        var pixelX = textureUv2.X;
                        var pixelY = textureUv2.Y;
                        var index = pixelY * width + pixelX;
                        pixels[index * 4 + 2] = (byte)(normal.X * 127 + 128);
                        pixels[index * 4 + 1] = (byte)(normal.Y * 127 + 128);
                        pixels[index * 4 + 0] = (byte)(normal.Z * 127 + 128);
                        pixels[index * 4 + 3] = 255;
                    }
                }
            });
        }

        bitmaps = bitmapTextures;
        heightmap = heightmapTexture;
        return new MeshLodDto<MeshVertex>(
            owner,
            indices.ToArray(),
            vertices,
            [new MeshSectionDto(0, 0, numFaces, false)],
            extraUvs,
            !weightmapColors.IsEmpty ? weightmapColors.Select(kvp => new MeshVertexColorDto(kvp.Key, kvp.Value)).ToArray() : null,
            1.0f);
    }
}
