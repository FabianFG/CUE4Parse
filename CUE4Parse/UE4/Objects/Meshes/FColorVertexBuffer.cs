using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Meshes
{
    public class FColorVertexBuffer
    {
        public readonly FColor[] Data;
        public readonly int Stride;
        public readonly int NumVertices;

        public FColorVertexBuffer(FArchive Ar)
        {
            var stripDataFlags = Ar.Ver >= UE4Version.VER_UE4_STATIC_SKELETAL_MESH_SERIALIZATION_FIX ? Ar.Read<FStripDataFlags>() : new FStripDataFlags();

            Stride = Ar.Read<int>();
            NumVertices = Ar.Read<int>();

            if (!stripDataFlags.IsDataStrippedForServer() & NumVertices > 0)
            {
                Data = Ar.ReadBulkArray<FColor>(() => Ar.Read<FColor>());
            }
            else
            {
                Data = new FColor[0];
            }
        }
    }
}
