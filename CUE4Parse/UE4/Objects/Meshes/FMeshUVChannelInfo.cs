using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Meshes
{
    public class FMeshUVChannelInfo
    {
        public bool bInitialized;
        public bool bOverrideDensities;
        public float[] LocalUVDensities;
        const int TEXSTREAM_MAX_NUM_UVCHANNELS = 4;

        public FMeshUVChannelInfo(FArchive Ar)
        {
            bInitialized = Ar.ReadBoolean();
            bOverrideDensities = Ar.ReadBoolean();
            LocalUVDensities = Ar.ReadArray<float>(TEXSTREAM_MAX_NUM_UVCHANNELS);
        }
    }
}