using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Engine.Model
{
    /**
    One vertex associated with a Bsp node's polygon.  Contains a vertex index
    into the level's FPoints table, and a unique number which is common to all
    other sides in the level which are cospatial with this side.
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
}