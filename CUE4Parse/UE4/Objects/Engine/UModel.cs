using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    /**
     * One vertex associated with a Bsp node's polygon.  Contains a vertex index
     * into the level's FPoints table, and a unique number which is common to all
     * other sides in the level which are cospatial with this side.
     */
    public readonly struct FVert : IUStruct
    {
        /** Index of a Vertex */
        public readonly int pVertex;

        /** If shared, index of unique side. Otherwise INDEX_NONE. */
        public readonly int iSide;

        /** The vertex's shadow map coordinate. */
        public readonly FVector2D ShadowTexCoord;

        /** The vertex's shadow map coordinate for the backface of the node. */
        public readonly FVector2D BackfaceShadowTexCoord;
    }

    /** Flags associated with a Bsp node. */
    public enum EBspNodeFlags : byte
    {
        // Flags.
        NF_NotCsg           = 0x01, // Node is not a Csg splitter, i.e. is a transparent poly.
        NF_NotVisBlocking   = 0x04, // Node does not block visibility, i.e. is an invisible collision hull.
        NF_BrightCorners    = 0x10, // Temporary.
        NF_IsNew            = 0x20, // Editor: Node was newly-added.
        NF_IsFront          = 0x40, // Filter operation bounding-sphere precomputed and guaranteed to be front.
        NF_IsBack           = 0x80, // Guaranteed back.
    }

    /**
     * FBspNode defines one node in the Bsp, including the front and back
     * pointers and the polygon data itself.  A node may have 0 or 3 to (MAX_NODE_VERTICES-1)
     * vertices. If the node has zero vertices, it's only used for splitting and
     * doesn't contain a polygon (this happens in the editor).
     *
     * vNormal, vTextureU, vTextureV, and others are indices into the level's
     * vector table.  iFront,iBack should be INDEX_NONE to indicate no children.
     *
     * If iPlane==INDEX_NONE, a node has no coplanars.  Otherwise iPlane
     * is an index to a coplanar polygon in the Bsp.  All polygons that are iPlane
     * children can only have iPlane children themselves, not fronts or backs.
     */
    public readonly struct FBspNode : IUStruct
    {
        public const int MAX_NODE_VERTICES = 255;
        public const int MAX_ZONES = 64;

        // Persistent information.
        public readonly FPlane Plane;  // 16 Plane the node falls into (X, Y, Z, W).
        public readonly int iVertPool; // 4  Index of first vertex in vertex pool, =iTerrain if NumVertices==0 and NF_TerrainFront.
        public readonly int iSurf;     // 4  Index to surface information.

        /** The index of the node's first vertex in the UModel's vertex buffer. */
        public readonly int iVertexIndex;

        /** The index in ULevel::ModelComponents of the UModelComponent containing this node. */
        public readonly ushort ComponentIndex;

        /** The index of the node in the UModelComponent's Nodes array. */
        public readonly ushort ComponentNodeIndex;

        /** The index of the element in the UModelComponent's Element array. */
        public readonly int ComponentElementIndex;

        // iBack:  4  Index to node in front (in direction of Normal).
        // iFront: 4  Index to node in back  (opposite direction as Normal).
        // iPlane: 4  Index to next coplanar poly in coplanar list.
        public readonly int iBack;
        public readonly int iFront;
        public readonly int iPlane;

        /** 4  Collision bound. */
        public readonly int iCollisionBound;

        /** 2 Visibility zone in 1=front, 0=back. */
        public readonly byte iZone0;
        public readonly byte iZone1;

        /**1  Number of vertices in node.*/
        public readonly byte NumVertices;

        /** 1  Node flags. */
        public readonly EBspNodeFlags NodeFlags;

        /**4  Leaf in back and front, INDEX_NONE=not a leaf.*/
        public readonly int iLeaf0;
        public readonly int iLeaf1;
    }

    public readonly struct FZoneSet : IUStruct
    {
        public readonly ulong MaskBits;
    }

    public readonly struct FZoneProperties : IUStruct
    {
        public readonly FPackageIndex ZoneActor;
        public readonly float LastRenderTime;
        public readonly FZoneSet Connectivity;
        public readonly FZoneSet Visibility;

        public FZoneProperties(FAssetArchive Ar)
        {
            ZoneActor = new FPackageIndex(Ar);
            Connectivity = Ar.Read<FZoneSet>();
            Visibility = Ar.Read<FZoneSet>();
            LastRenderTime = Ar.Read<float>();
        }
    }

    /**
     * One Bsp polygon.  Lists all of the properties associated with the
     * polygon's plane.  Does not include a point list; the actual points
     * are stored along with Bsp nodes, since several nodes which lie in the
     * same plane may reference the same poly.
     */
    public readonly struct FBspSurf : IUStruct
    {
        public readonly FPackageIndex Material; // UMaterialInterface
        public readonly uint PolyFlags; // Polygon flags.
        public readonly int pBase; // Polygon & texture base point index (where U,V==0,0).
        public readonly int vNormal; // Index to polygon normal.
        public readonly int vTextureU; // Texture U-vector index.
        public readonly int vTextureV; // Texture V-vector index.
        public readonly int iBrushPoly; // Editor brush polygon index.
        public readonly FPackageIndex Actor; // ABrush Brush actor owning this Bsp surface.
        public readonly FPlane Plane; // The plane this surface lies on.
        public readonly float LightMapScale; // The number of units/lightmap texel on this surface.
        public readonly int iLightmassIndex; // Index to the lightmass settings

        public FBspSurf(FAssetArchive Ar)
        {
            Material = new FPackageIndex(Ar);
            PolyFlags = Ar.Read<uint>();
            pBase = Ar.Read<int>();
            vNormal = Ar.Read<int>();
            vTextureU = Ar.Read<int>();
            vTextureV = Ar.Read<int>();
            iBrushPoly = Ar.Read<int>();
            Actor = new FPackageIndex(Ar);
            Plane = Ar.Read<FPlane>();
            LightMapScale = Ar.Read<float>();
            iLightmassIndex = Ar.Read<int>();
        }
    }

    public struct FModelVertex : IUStruct
    {
        public FVector Position;
        public FVector TangentX;
        public FVector4 TangentZ;
        public FVector2D TexCoord;
        public FVector2D ShadowTexCoord;

        public FModelVertex(FArchive Ar)
        {
            Position = Ar.Read<FVector>();

            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.IncreaseNormalPrecision)
            {
                TangentX = (FVector) Ar.Read<FDeprecatedSerializedPackedNormal>();
                TangentZ = (FVector4) Ar.Read<FDeprecatedSerializedPackedNormal>();
            }
            else
            {
                TangentX = Ar.Read<FVector>();
                TangentZ = Ar.Read<FVector4>();
            }

            TexCoord = Ar.Read<FVector2D>();
            ShadowTexCoord = Ar.Read<FVector2D>();
        }

        public FVector GetTangentY() => ((FVector) TangentZ ^ TangentX) * TangentZ.W;
    }

    public struct FDeprecatedModelVertex : IUStruct
    {
        public FVector Position;
        public FDeprecatedSerializedPackedNormal TangentX;
        public FDeprecatedSerializedPackedNormal TangentZ;
        public FVector2D TexCoord;
        public FVector2D ShadowTexCoord;

        public static implicit operator FModelVertex(FDeprecatedModelVertex v) => new()
        {
            Position = v.Position,
            TangentX = (FVector) v.TangentX,
            TangentZ = (FVector4) v.TangentZ,
            TexCoord = v.TexCoord,
            ShadowTexCoord = v.ShadowTexCoord
        };
    }

    /** A vertex buffer for a set of BSP nodes. */
    public class FModelVertexBuffer : IUStruct
    {
        public readonly FModelVertex[] Vertices;

        public FModelVertexBuffer(FArchive Ar)
        {
            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.IncreaseNormalPrecision)
            {
                var deprecatedVertices = Ar.ReadBulkArray<FDeprecatedModelVertex>();
                Vertices = new FModelVertex[deprecatedVertices.Length];
                for (int i = 0; i < Vertices.Length; i++)
                {
                    Vertices[i] = deprecatedVertices[i];
                }
            }
            else
            {
                Vertices = Ar.ReadArray(() => new FModelVertex(Ar));
            }
        }
    }

    public class UModel : Assets.Exports.UObject
    {
        public FBoxSphereBounds Bounds;
        public FVector[] Vectors;
        public FVector[] Points;
        public FBspNode[] Nodes;
        public FBspSurf[] Surfs;
        public FVert[] Verts;
        public int NumSharedSides;
        public bool RootOutside;
        public bool Linked;
        public uint NumUniqueVertices;
        public FModelVertexBuffer VertexBuffer;
        public FGuid LightingGuid;
        public FLightmassPrimitiveSettings[] LightmassSettings;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            const int StripVertexBufferFlag = 1;
            var stripData = new FStripDataFlags(Ar);

            Bounds = new FBoxSphereBounds(Ar);

            Vectors = Ar.ReadBulkArray<FVector>();
            Points = Ar.ReadBulkArray<FVector>();
            Nodes = Ar.ReadBulkArray<FBspNode>();

            if (Ar.Ver < EUnrealEngineObjectUE4Version.BSP_UNDO_FIX)
            {
                var surfsOwner = new FPackageIndex(Ar);
                Surfs = Ar.ReadArray(() => new FBspSurf(Ar));
            }
            else
            {
                Surfs = Ar.ReadArray(() => new FBspSurf(Ar));
            }
            Verts = Ar.ReadBulkArray<FVert>();

            NumSharedSides = Ar.Read<int>();
            if (Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_ZONES_FROM_MODEL)
            {
                var dummyZones = Ar.ReadArray<FZoneProperties>();
            }

            var bHasEditorOnlyData = !Ar.IsFilterEditorOnly || Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_UNUSED_UPOLYS_FROM_UMODEL;
            if (bHasEditorOnlyData)
            {
                var dummyPolys = new FPackageIndex(Ar);
                Ar.SkipBulkArrayData(); // DummyLeafHulls
                Ar.SkipBulkArrayData(); // DummyLeaves
            }

            RootOutside = Ar.ReadBoolean();
            Linked = Ar.ReadBoolean();

            if (Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_ZONES_FROM_MODEL)
            {
                var dummyPortalNodes = Ar.ReadBulkArray<int>();
            }

            NumUniqueVertices = Ar.Read<uint>();

            if (!stripData.IsEditorDataStripped() || !stripData.IsClassDataStripped(StripVertexBufferFlag))
            {
                VertexBuffer = new FModelVertexBuffer(Ar);
            }

            LightingGuid = Ar.Read<FGuid>();
            LightmassSettings = Ar.ReadArray(() => new FLightmassPrimitiveSettings(Ar));
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Bounds");
            serializer.Serialize(writer, Bounds);

            writer.WritePropertyName("Vectors");
            serializer.Serialize(writer, Vectors);

            writer.WritePropertyName("Points");
            serializer.Serialize(writer, Points);

            writer.WritePropertyName("Nodes");
            serializer.Serialize(writer, Nodes);

            writer.WritePropertyName("Surfs");
            serializer.Serialize(writer, Surfs);

            writer.WritePropertyName("NumSharedSides");
            serializer.Serialize(writer, NumSharedSides);

            writer.WritePropertyName("VertexBuffer");
            serializer.Serialize(writer, VertexBuffer);

            writer.WritePropertyName("LightingGuid");
            serializer.Serialize(writer, LightingGuid);

            writer.WritePropertyName("LightmassSettings");
            serializer.Serialize(writer, LightmassSettings);
        }
    }
}
