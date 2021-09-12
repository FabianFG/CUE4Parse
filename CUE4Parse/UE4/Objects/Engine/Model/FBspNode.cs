using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Engine.Model
{   
    /**
    FBspNode defines one node in the Bsp, including the front and back
    pointers and the polygon data itself.  A node may have 0 or 3 to (MAX_NODE_VERTICES-1)
    vertices. If the node has zero vertices, it's only used for splitting and
    doesn't contain a polygon (this happens in the editor).

    vNormal, vTextureU, vTextureV, and others are indices into the level's
    vector table.  iFront,iBack should be INDEX_NONE to indicate no children.

    If iPlane==INDEX_NONE, a node has no coplanars.  Otherwise iPlane
    is an index to a coplanar polygon in the Bsp.  All polygons that are iPlane
    children can only have iPlane children themselves, not fronts or backs.
    */
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FBspNode : IUStruct
    {
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
        public fixed byte iZone[2];
        
        /**1  Number of vertices in node.*/
        public readonly byte NumVertices;
        
        /** 1  Node flags. */
        public EBspNodeFlags NodeFlags;

        /**4  Leaf in back and front, INDEX_NONE=not a leaf.*/
        public fixed int iLeaf[2];

        // idk how to do it like unreal
        public int GetMaxZones()
        {
            return 64;
        }

        public int GetMaxNodeVertices()
        {
            return 255;
        }
    }

    public enum EBspNodeFlags: byte
    {
        // Flags.
        NF_NotCsg			= 0x01, // Node is not a Csg splitter, i.e. is a transparent poly.
        NF_NotVisBlocking   = 0x04, // Node does not block visibility, i.e. is an invisible collision hull.
        NF_BrightCorners	= 0x10, // Temporary.
        NF_IsNew 		 	= 0x20, // Editor: Node was newly-added.
        NF_IsFront     		= 0x40, // Filter operation bounding-sphere precomputed and guaranteed to be front.
        NF_IsBack      		= 0x80, // Guaranteed back.
    };
}