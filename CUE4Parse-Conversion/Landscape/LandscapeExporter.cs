using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using Mesh = CUE4Parse_Conversion.Meshes.Mesh;


namespace CUE4Parse_Conversion.Landscape;

[Flags]
public enum ELandscapeExportFlags
{
    ExportHeightmap = 1 << 0,
    ExportWeightmap = 1 << 1,
    ExportLandscapeMesh = 1 << 3,
    ExportAll = ExportHeightmap | ExportWeightmap | ExportLandscapeMesh
}

public class LandscapeExporter : ExporterBase
{
    private ELandscapeExportFlags _flags;
    private bool _isInfoSet;
    private ULandscapeComponent[] _landscapeComponents;
    private int _componentSize;
    private FPackageIndex _landscapeMaterial;
    public FGuid LandscapeGuid { get; private set; }
    private List<MaterialExporter2> Materials = new List<MaterialExporter2>();

    internal Dictionary<string, SKBitmap> WeightMaps { get; set; } = new();
    internal Dictionary<string, Image> HeightMaps { get; set; } = new();

    internal Mesh[]? ProcessedFiles;

    public LandscapeExporter(ALandscapeProxy landscape, ULandscapeComponent[]? components, ExporterOptions options,
        ELandscapeExportFlags flags = ELandscapeExportFlags.ExportAll) : base(landscape, options)
    {
        _isInfoSet = false;
        _flags = flags;
        _landscapeComponents = [];

        LandscapeGuid = landscape.LandscapeGuid;

        _landscapeMaterial = landscape.LandscapeMaterial;
        _componentSize = landscape.ComponentSizeQuads;

        _landscapeComponents = components ?? LoadComponents(landscape);
        SetMeshData(DoThings3_Mesh());
    }

#if DEBUG
    public LandscapeExporter(UWorld world, ExporterOptions options) : base(world, options)
    {
        // _isInfoSet = false;;
        _landscapeComponents = Array.Empty<ULandscapeComponent>();
        InitComponentsFromWorld(world);
        SetMeshData(DoThings3_Mesh());
    }
#endif

    private void SetMeshData(CStaticMeshLod? lod)
    {
        var final = new List<Mesh>();
        var path = GetExportSavePath();

        if (_flags.HasFlag(ELandscapeExportFlags.ExportLandscapeMesh))
        {
            Debug.Assert(lod != null, nameof(lod) + " != null");
            using var Ar = new FArchiveWriter();
            var materialExports = Options.ExportMaterials ? new List<MaterialExporter2>() : null;
            string ext;
            switch (Options.MeshFormat)
            {
                case EMeshFormat.ActorX:
                    ext = "pskx";
                    new ActorXMesh(lod, materialExports, Array.Empty<FPackageIndex>(), Options).Save(Ar);
                    break;
                case EMeshFormat.Gltf2:
                    ext = "glb";
                    new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                    break;
                case EMeshFormat.OBJ:
                    ext = "obj";
                    new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                    break;
                case EMeshFormat.UEFormat: {
                    ext = "uemodel";

                    var mesh = new CStaticMesh(); // aaaaaaaa
                    mesh.LODs.Add(lod);
                    new UEModel(ExportName, mesh, new FPackageIndex(), Options).Save(Ar);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
            }

            final.Add(new Mesh($"{path}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
        }

        if (_flags.HasFlag(ELandscapeExportFlags.ExportWeightmap))
        {
            foreach (var kv in WeightMaps)
            {
                string weightMapPath = $"{path}/{kv.Key}.png";
                var weightMap = kv.Value;
                var ImageData = weightMap.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                final.Add(new Mesh(weightMapPath, ImageData, new List<MaterialExporter2>()));
            }
        }

        if (_flags.HasFlag(ELandscapeExportFlags.ExportHeightmap))
        {
            foreach (var kv in HeightMaps)
            {
                string heightMapPath = $"{path}/{kv.Key}.png";
                var stream = new MemoryStream();
                kv.Value.Save(stream, new PngEncoder());
                final.Add(new Mesh(heightMapPath, stream.GetBuffer(), new List<MaterialExporter2>()));
                stream.Dispose();
            }
        }

        final.Add(new Mesh($"{path}/Guid_{LandscapeGuid}", Encoding.UTF8.GetBytes(LandscapeGuid.ToString()), []));

        ProcessedFiles = final.ToArray();
        WeightMaps.Clear();
        final.Clear();
    }

    private void InitComponentsFromWorld(UWorld world)
    {
#if !DEBUG
         throw new InvalidOperationException();
#endif
        var comps = new List<ULandscapeComponent>();
        var actors = world.PersistentLevel.Load<ULevel>()!.Actors;
        foreach (var t in actors)
        {
            var actor = t.Load();
            if (actor is not ALandscapeProxy proxy)
                continue;
            comps.AddRange(LoadComponents(proxy));
            _componentSize = proxy.ComponentSizeQuads;
        }

        foreach (var level in world.StreamingLevels)
        {
            var uWorld = level.Load()?.Get<UWorld>("WorldAsset");
            var persLevel = uWorld?.PersistentLevel.Load<ULevel>();
            for (var j = 0; j < persLevel!.Actors.Length; j++)
            {
                var actor = persLevel.Actors[j].Load();
                if (actor is not ALandscapeProxy proxy)
                    continue;
                comps.AddRange(LoadComponents(proxy));
                _componentSize = proxy.ComponentSizeQuads;
            }
        }

        _landscapeComponents = comps.ToArray();
    }

    private ULandscapeComponent[] LoadComponents(ALandscapeProxy? loadedActor)
    {
        if (loadedActor != null)
        {
            var comps = loadedActor.LandscapeComponents;
            var resComponents = new ULandscapeComponent[comps.Length];
            for (var i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                resComponents[i] = comp.Load<ULandscapeComponent>()!;
            }

            return resComponents.ToArray();
        }

        return [];
    }

    private CStaticMeshLod? DoThings3_Mesh()
    {
        int MinX = int.MaxValue, MinY = int.MaxValue;
        int MaxX = int.MinValue, MaxY = int.MinValue;

        foreach (var comp in _landscapeComponents)
        {
            if (_componentSize == -1)
                _componentSize = comp.ComponentSizeQuads;
            else
            {
                Debug.Assert(_componentSize == comp.ComponentSizeQuads);
            }

            comp.GetComponentExtent(ref MinX, ref MinY, ref MaxX, ref MaxY);
        }

        // Create and fill in the vertex position data source.
        int componentSizeQuads = ((_componentSize + 1) >> 0 /*Landscape->ExportLOD*/) - 1;
        float scaleFactor = (float)componentSizeQuads / _componentSize;
        int numComponents = _landscapeComponents.Length;
        int vertexCountPerComponent = (componentSizeQuads + 1) * (componentSizeQuads + 1);
        int vertexCount = numComponents * vertexCountPerComponent;
        int triangleCount = numComponents * (componentSizeQuads * componentSizeQuads) * 2;

        FVector2D uvScale = new FVector2D(1.0f, 1.0f) / new FVector2D((MaxX - MinX) + 1, (MaxY - MinY) + 1);

        // For image export
        int width = MaxX - MinX + 1;
        int height = MaxY - MinY + 1;

        CStaticMeshLod? landscapeLod = null;
        if (_flags.HasFlag(ELandscapeExportFlags.ExportLandscapeMesh))
        {
            landscapeLod = new CStaticMeshLod();
            landscapeLod.NumTexCoords = 2; // TextureUV and weightmapUV
            landscapeLod.AllocateVerts(vertexCount);
            landscapeLod.AllocateVertexColorBuffer();
        }

        var extraVertexColorMap = new ConcurrentDictionary<string, CVertexColor>();

        var weightMaps = new Dictionary<string, SKBitmap>();
        var weightMapsPixels = new Dictionary<int, IntPtr>();
        var weightMapLock = new object();
        var heightMapData = new L16[height * width];

        // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4549
        var tasks = new Task[_landscapeComponents.Length];
        for (int i = 0, selectedComponentIndex = 0; i < _landscapeComponents.Length; i++)
        {
            var comp = _landscapeComponents[i];

            var CDI = new FLandscapeComponentDataInterface(comp, 0);
            CDI.EnsureWeightmapTextureDataCache();

            int baseVertIndex = selectedComponentIndex++ * vertexCountPerComponent;

            var weightMapAllocs = comp.GetWeightmapLayerAllocations();

            var compTransform = comp.GetComponentTransform();
            var relLoc = comp.GetRelativeLocation();

            var task = Task.Run(() =>
            {
                for (int vertIndex = 0; vertIndex < vertexCountPerComponent; vertIndex++)
                {
                    CDI.VertexIndexToXY(vertIndex, out var vertX, out var vertY);

                    var vertCoord = CDI.GetLocalVertex(vertX, vertY);
                    var position = vertCoord + relLoc;

                    CDI.GetLocalTangentVectors(vertIndex, out var tangentX, out var biNormal, out var normal);

                    normal /= compTransform.Scale3D;
                    normal.Normalize();
                    FVector4.AsFVector(ref tangentX) /= compTransform.Scale3D;
                    FVector4.AsFVector(ref tangentX).Normalize();
                    biNormal /= compTransform.Scale3D;
                    biNormal.Normalize();

                    var textureUv = new FVector2D(vertX * scaleFactor + comp.SectionBaseX,
                        vertY * scaleFactor + comp.SectionBaseY);
                    var textureUv2 = new TIntVector2<int>((int)textureUv.X - MinX, (int)textureUv.Y - MinY);

                    var weightmapUv = (textureUv - new FVector2D(MinX, MinY)) * uvScale;

                    heightMapData[textureUv2.X + textureUv2.Y * width] = new L16((ushort)(CDI.GetVertex(vertX, vertY) + relLoc).Z);

                    foreach (var allocationInfo in weightMapAllocs)
                    {
                        var weight = CDI.GetLayerWeight(vertX, vertY, allocationInfo);
                        if (weight == 0) continue;

                        var layerName = allocationInfo.GetLayerName();

                        // weight as Mesh Vertex colors
                        if (_flags.HasFlag(ELandscapeExportFlags.ExportLandscapeMesh))
                        {
                            // ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd
                            if (!extraVertexColorMap.ContainsKey(layerName))
                            {
                                var shortName = layerName.SubstringBefore("_LayerInfo");
                                shortName = shortName.Substring(0, Math.Min(20 - 6, shortName.Length));
                                extraVertexColorMap.TryAdd(layerName, new CVertexColor(shortName, new FColor[vertexCount]));
                            }

                            extraVertexColorMap[layerName].ColorData[baseVertIndex + vertIndex] = new FColor(weight, weight, weight, weight);
                        }

                        var pixelX = textureUv2.X;
                        var pixelY = textureUv2.Y;

                        if (_flags.HasFlag(ELandscapeExportFlags.ExportWeightmap))
                        {
                            lock (weightMapLock)
                            {
                                if (!weightMaps.ContainsKey(layerName))
                                {
                                    var bitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Unpremul);
                                    weightMaps.TryAdd(layerName, bitmap);
                                    weightMapsPixels.TryAdd(allocationInfo.GetLayerNameHash(), bitmap.GetPixels());
                                }
                            }

                            // weightMaps[layerName].SetPixel((int)pixelX, (int)pixelY, new SKColor(weight, weight, weight, 255)); // slow
                            unsafe
                            {
                                var pixels =
                                    (byte*)weightMapsPixels[
                                        allocationInfo
                                            .GetLayerNameHash()]; // possible race condition but it doesn't matter
                                pixels[pixelY * width + pixelX] = weight;
                            }

                            // debug color
                            // var infoObject = allocationInfo.LayerInfo.Load<ULandscapeLayerInfoObject>();
                            // var cl = infoObject.LayerUsageDebugColor.ToFColor(true);
                            // weightMapsData[allocationInfo.LayerInfo.Name].SetPixel((int)pixel_x, (int)pixel_y, new SKColor(cl.R, cl.G, cl.B, weight));
                        }
                    }

                    if (_flags.HasFlag(ELandscapeExportFlags.ExportLandscapeMesh) && landscapeLod != null)
                    {
                        var vert = landscapeLod.Verts[baseVertIndex + vertIndex];
                        vert.Position = position;
                        vert.Normal = new FVector4(normal); // this might be broken
                        vert.Tangent = tangentX;
                        vert.UV = (FMeshUVFloat)textureUv;

                        landscapeLod.ExtraUV.Value[0][baseVertIndex + vertIndex] = (FMeshUVFloat)weightmapUv;
                    }
                }
            });
            tasks[i] = task;
        }

        Task.WaitAll(tasks);

        // image.Save(File.OpenWrite("heightmap.png"), new PngEncoder());
        if (_flags.HasFlag(ELandscapeExportFlags.ExportHeightmap))
        {
            var image = Image.LoadPixelData<L16>(heightMapData, width, height);
            HeightMaps.Add("heightmap", image);
        }

        // skimage
        //var heightMap = new SKBitmap(Width, Height, SKColorType.RgbaF16, SKAlphaType.Unpremul);
        // heightMap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(File.OpenWrite("heightmap.png"));
        if (_flags.HasFlag(ELandscapeExportFlags.ExportWeightmap))
        {
            WeightMaps = weightMaps.ToDictionary(x => x.Key, x => x.Value);
        }

        if (_flags.HasFlag(ELandscapeExportFlags.ExportLandscapeMesh) && landscapeLod != null)
        {
            landscapeLod.ExtraVertexColors = extraVertexColorMap.Values.ToArray();
            extraVertexColorMap.Clear();
            var mat = _landscapeMaterial.Load<UMaterialInterface>();
            landscapeLod.Sections = new Lazy<CMeshSection[]>(new[] {
                new CMeshSection(0, 0, triangleCount, mat?.Name ?? "DefaultMaterial", _landscapeMaterial.ResolvedObject)
            });
        }
        else
        {
            return null;
        }

        var meshIndices = new List<uint>(triangleCount * 3); // TODO: replace with ArrayPool.Shared.Rent
        // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4657
        for (int componentIndex = 0; componentIndex < numComponents; componentIndex++)
        {
            int baseVertIndex = componentIndex * vertexCountPerComponent;

            for (int Y = 0; Y < componentSizeQuads; Y++)
            {
                for (int X = 0; X < componentSizeQuads; X++)
                {
                    if (true) // (VisibilityData[BaseVertIndex + Y * (ComponentSizeQuads + 1) + X] < VisThreshold)
                    {
                        var w1 = baseVertIndex + (X + 0) + (Y + 0) * (componentSizeQuads + 1);
                        var w2 = baseVertIndex + (X + 1) + (Y + 1) * (componentSizeQuads + 1);
                        var w3 = baseVertIndex + (X + 1) + (Y + 0) * (componentSizeQuads + 1);

                        meshIndices.Add((uint)w1);
                        meshIndices.Add((uint)w2);
                        meshIndices.Add((uint)w3);

                        var w4 = baseVertIndex + (X + 0) + (Y + 0) * (componentSizeQuads + 1);
                        var w5 = baseVertIndex + (X + 0) + (Y + 1) * (componentSizeQuads + 1);
                        var w6 = baseVertIndex + (X + 1) + (Y + 1) * (componentSizeQuads + 1);

                        meshIndices.Add((uint)w4);
                        meshIndices.Add((uint)w5);
                        meshIndices.Add((uint)w6);
                    }
                }
            }
        }

        landscapeLod.Indices =
            new Lazy<FRawStaticIndexBuffer>(new FRawStaticIndexBuffer { Indices32 = meshIndices.ToArray() });
        meshIndices.Clear();

        return landscapeLod;
    }

    public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
    {
        Debug.Assert(ProcessedFiles != null, nameof(ProcessedFiles) + " != null");
        var b = false;
        label = string.Empty;
        savedFilePath = string.Empty;
        foreach (var pf in ProcessedFiles.Reverse())
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