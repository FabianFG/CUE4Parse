using System.Buffers;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FNaniteResources
{
    // Persistent State
    public FByteBulkData? StreamablePages = null; // Remaining pages are streamed on demand.
    [JsonIgnore] public ushort[] ImposterAtlas = [];
    [JsonIgnore] public FPackedHierarchyNode[] HierarchyNodes = [];
    [JsonIgnore] public uint[] HierarchyRootOffsets = [];
    [JsonIgnore] public FPageStreamingState[] PageStreamingStates = [];
    [JsonIgnore] public uint[] PageDependencies = [];
    public FMatrix3x4[] AssemblyTransforms = [];
    public uint[] AssemblyBoneAttachmentData;
    public uint[] BoneIndices;
    public ulong VoxelMaterialsMask;
    public FBoxSphereBounds? MeshBounds = null; // FBoxSphereBounds3f

    /// <summary>
    /// Dictionary of page ranges relevant to streaming requests and fixups
    /// </summary>
    public FPageRangeKey[] PageRangeLookup;

    /// <summary>The number of root pages found outside of the bulk page.</summary>
    public int NumRootPages = 0;
    /// <summary>The precision which which vertex positions are recorded with.</summary>
    public int PositionPrecision = 0;
    /// <summary>The precision which which vertex normals are recorded with. Added with 5.2.</summary>
    public int NormalPrecision = 0;
    /// <summary>The number of triangles the original mesh had.</summary>
    public uint NumInputTriangles = 0;
    /// <summary>The number of verticies the original mesh had.</summary>
    public uint NumInputVertices = 0;
    public ushort NumInputMeshes = 0;
    /// <summary>The number of UVs used by the origina mesh.</summary>
    public ushort NumInputTexCoords = 0;
    /// <summary>The number of clusters in total for this mesh.</summary>
    public uint NumClusters = 0;

    public uint NumInputCurves;
    [JsonConverter(typeof(StringEnumConverter))]
    public NaniteConstants.NANITE_RESOURCE_FLAG ResourceFlags = 0;

    [JsonIgnore] public FNaniteStreamableData?[] LoadedPages = [];
    [JsonIgnore] public readonly FAssetArchive Archive;
    [JsonIgnore] private byte[] RootData = [];
    private List<uint> FailedPages = [];

    public FNaniteResources(FAssetArchive Ar)
    {
        Archive = Ar;
        var stripFlags = new FStripDataFlags(Ar);
        if (!stripFlags.IsAudioVisualDataStripped())
        {
            ResourceFlags = Ar.Read<NaniteConstants.NANITE_RESOURCE_FLAG>();
            StreamablePages = new FByteBulkData(Ar);
            RootData = Ar.ReadArray<byte>();
            PageStreamingStates = Ar.ReadArray(() => new FPageStreamingState(Ar));
            HierarchyNodes = Ar.ReadArray(() => new FPackedHierarchyNode(Ar));
            HierarchyRootOffsets = Ar.ReadArray<uint>();
            PageDependencies = Ar.ReadArray(() => Ar.Game >= GAME_UE5_7 ? Ar.Read<ushort>() : Ar.Read<uint>());
            if (Ar.Game >= GAME_UE5_6)
            {
                AssemblyTransforms = Ar.ReadArray<FMatrix3x4>();

                if (Ar.Game >= GAME_UE5_7)
                {
                    AssemblyBoneAttachmentData = Ar.ReadArray<uint>();
                    if (Ar.Game >= GAME_UE5_8)
                        BoneIndices = Ar.ReadArray<uint>();

                    PageRangeLookup = Ar.ReadArray<FPageRangeKey>();
                }

                MeshBounds = new FBoxSphereBounds(Ar.Read<FVector>(), Ar.Read<FVector>(), Ar.Read<float>());
            }
            if (Ar.Game < GAME_UE5_8) ImposterAtlas = Ar.ReadArray<ushort>();
            if (Ar.Game is GAME_Aion2) Ar.SkipFixedArray(1); // same length as ImposterAtlas
            NumRootPages = Ar.Read<int>();
            PositionPrecision = Ar.Read<int>();
            if (Ar.Game >= GAME_UE5_2) NormalPrecision = Ar.Read<int>();
            NumInputTriangles = Ar.Read<uint>();
#if DEBUG
            if (NumInputTriangles > 0)
            {
                Log.Debug("Nanite mesh has {NumInputTriangles} triangles", NumInputTriangles);
            }
#endif
            NumInputVertices = Ar.Read<uint>();
            if (Ar.Game < GAME_UE5_6)
            {
                NumInputMeshes = Ar.Read<ushort>();
                NumInputTexCoords = Ar.Read<ushort>();
            }
            if (Ar.Game >= GAME_UE5_1) NumClusters = Ar.Read<uint>();
            if (Ar.Game >= GAME_UE5_7) VoxelMaterialsMask = Ar.Read<ulong>();
            if (Ar.Game >= GAME_UE5_8) NumInputCurves = Ar.Read<uint>();
        }
    }

    public void LoadAllPages()
    {
        if (Archive.Owner?.Provider?.ReadNaniteData == true && PageStreamingStates.Length > 0)
        {
            LoadedPages = new FNaniteStreamableData[PageStreamingStates.Length];
            for (uint i = 0; i < PageStreamingStates.Length; i++)
            {
                GetPage(i);
            }
        }
    }

    public FNaniteStreamableData? GetPage(uint pageIndex)
    {
        if (pageIndex >= PageStreamingStates.Length) return null;

        if (LoadedPages[pageIndex] == null && !FailedPages.Contains(pageIndex))
        {
            if (TryLoadPage(pageIndex, out var page))
                LoadedPages[pageIndex] = page;
            else
                FailedPages.Add(pageIndex);
        }

        return LoadedPages[pageIndex];
    }

    public bool TryLoadPage(uint pageIndex, out FNaniteStreamableData? outPage)
    {
        if (pageIndex >= LoadedPages.Length)
        {
            Log.Error("PageIndex {pageIndex} is out of range!", pageIndex);
            outPage = null;
            return false;
        }

        if (LoadedPages[pageIndex] is not null)
        {
            outPage = LoadedPages[pageIndex];
            return true;
        }

        var versionContainer = Archive.Versions;
        if (Archive.Game == GAME_TheFirstDescendant)
        {
            versionContainer = (VersionContainer) versionContainer.Clone();
            versionContainer.Game = GAME_UE5_3;
        }

        var page = PageStreamingStates[pageIndex];
        byte[] buffer = ArrayPool<byte>.Shared.Rent((int)page.BulkSize);
        try
        {
            if (pageIndex < NumRootPages)
            {
                Buffer.BlockCopy(RootData, (int) page.BulkOffset, buffer, 0, (int) page.BulkSize);
            }
            else
            {
                Buffer.BlockCopy(StreamablePages.Data, (int) page.BulkOffset, buffer, 0, (int) page.BulkSize);
            }

            using var pageArchive = new FByteArchive($"NaniteStreamablePage{pageIndex}", buffer, versionContainer);
            outPage = new FNaniteStreamableData(pageArchive, this, pageIndex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load Nanite page {pageIndex}!", pageIndex);
            outPage = null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return outPage != null;
    }
}
