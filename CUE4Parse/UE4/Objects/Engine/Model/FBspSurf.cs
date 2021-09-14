using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Model
{
    /**
    One Bsp polygon.  Lists all of the properties associated with the
    polygon's plane.  Does not include a point list; the actual points
    are stored along with Bsp nodes, since several nodes which lie in the
    same plane may reference the same poly.
    */
    [StructLayout(LayoutKind.Sequential)]
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
}