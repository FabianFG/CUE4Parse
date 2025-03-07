using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[JsonConverter(typeof(FSkeletalMeshVertexColorBufferConverter))]
public class FSkeletalMeshVertexColorBuffer
{
    public FColor[] Data;

    public FSkeletalMeshVertexColorBuffer()
    {
        Data = [];
    }

    public FSkeletalMeshVertexColorBuffer(FArchive Ar)
    {
        var stripDataFlags = new FStripDataFlags(Ar, FPackageFileVersion.CreateUE4Version(EUnrealEngineObjectUE4Version.STATIC_SKELETAL_MESH_SERIALIZATION_FIX));
        Data = !stripDataFlags.IsAudioVisualDataStripped() ? Ar.ReadBulkArray<FColor>() : [];
    }

    public FSkeletalMeshVertexColorBuffer(FColor[] data)
    {
        Data = data;
    }
}
