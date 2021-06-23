using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Objects.RenderCore;
using System.Linq;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FStaticMeshUVItem
    {
        public readonly FPackedNormal[] Normal;
        public readonly FMeshUVFloat[] UV;

        public FStaticMeshUVItem(FArchive Ar, bool useHighPrecisionTangents, int numStaticUVSets, bool useStaticFloatUVs)
        {
            Normal = SerializeTangents(Ar, useHighPrecisionTangents);
            UV = SerializeTexcoords(Ar, numStaticUVSets, useStaticFloatUVs);
        }

        public FStaticMeshUVItem(FPackedNormal[] normal, FMeshUVFloat[] uv)
        {
            Normal = normal;
            UV = uv;
        }

        public static FPackedNormal[] SerializeTangents(FArchive Ar, bool useHighPrecisionTangents)
        {
            if (!useHighPrecisionTangents)
                return new FPackedNormal[] { new FPackedNormal(Ar), new FPackedNormal(0), new FPackedNormal(Ar) }; // # TangentX and TangentZ

            return new FPackedNormal[] { (FPackedNormal)new FPackedRGBA16N(Ar), new FPackedNormal(0), (FPackedNormal)new FPackedRGBA16N(Ar) };

        }

        public static FMeshUVFloat[] SerializeTexcoords(FArchive Ar, int numStaticUVSets, bool useStaticFloatUVs)
        {
            if (useStaticFloatUVs)
                return Enumerable.Repeat(new FMeshUVFloat(Ar), numStaticUVSets).ToArray();
            return Enumerable.Repeat((FMeshUVFloat)new FMeshUVHalf(Ar), numStaticUVSets).ToArray();
        }
    }
}
