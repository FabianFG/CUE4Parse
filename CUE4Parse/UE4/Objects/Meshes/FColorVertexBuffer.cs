using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Meshes;

[JsonConverter(typeof(FColorVertexBufferConverter))]
public class FColorVertexBuffer
{
    public readonly FColor[] Data;
    public readonly int Stride;
    public readonly int NumVertices;

    public FColorVertexBuffer()
    {
        Data = [];
    }

    public FColorVertexBuffer(FArchive Ar)
    {
        var stripDataFlags = new FStripDataFlags(Ar, FPackageFileVersion.CreateUE4Version(EUnrealEngineObjectUE4Version.STATIC_SKELETAL_MESH_SERIALIZATION_FIX));

        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();

        if (!stripDataFlags.IsAudioVisualDataStripped() & NumVertices > 0)
        {
            Data = Ar.ReadBulkArray<FColor>();
        }
        else
        {
            Data = [];
        }
    }
}
