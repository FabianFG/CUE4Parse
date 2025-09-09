using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.GeometryFramework;

public class UDynamicMesh : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.DynamicMeshCompactedSerialization)
        {
            //SerializeInternal<InitialVersion>(Ar, nullptr);
            //Mesh = new FDynamicMesh3(Ar, options);
        }
        else
        {
            var options = new FDynamicMesh3SerializationOptions(Ar);
            //Mesh = new FDynamicMesh3(Ar);
        }
    }

    // Encapsulates our serialization options, and selects the serialization variant for a given set of options. 
    public struct FDynamicMesh3SerializationOptions(FArchive Ar)
    {
        public bool bPreserveDataLayout = Ar.ReadBoolean(); //< Preserve the data layout, i.e. external vertex/triangle/edge indices are still valid after roundtrip serialization.
        public bool bCompactData = Ar.ReadBoolean(); //< Remove any holes or padding in the data layout, and discard/recompute any redundant data. 
        public bool bUseCompression = Ar.ReadBoolean(); //< Compress all data buffers to minimize memory footprint.
    }
}
