using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Textures
{
    public enum EVirtualTextureCodec : byte
    {
        Black,			//Special case codec, always outputs black pixels 0,0,0,0
        OpaqueBlack,	//Special case codec, always outputs opaque black pixels 0,0,0,255
        White,			//Special case codec, always outputs white pixels 255,255,255,255
        Flat,			//Special case codec, always outputs 128,125,255,255 (flat normal map)
        RawGPU,			//Uncompressed data in an GPU-ready format (e.g R8G8B8A8, BC7, ASTC, ...)
        ZippedGPU,		//Same as RawGPU but with the data zipped
        Crunch,			//Use the Crunch library to compress data
        Max,			// Add new codecs before this entry
    };

    public class FVirtualTextureDataChunk
    {
        public readonly FByteBulkData BulkData;
        public readonly uint SizeInBytes;
        public readonly uint CodecPayloadSize;
        public readonly ushort[] CodecPayloadOffset;
        public readonly EVirtualTextureCodec[] CodecType;

        public FVirtualTextureDataChunk(FAssetArchive Ar, uint numLayers)
        {
            CodecType = new EVirtualTextureCodec[numLayers];
            CodecPayloadOffset = new ushort[numLayers];

            SizeInBytes = Ar.Read<uint>();
            CodecPayloadSize = Ar.Read<uint>();
            for (uint layerIndex = 0u; layerIndex < numLayers; ++layerIndex)
            {
                CodecType[layerIndex] = Ar.Read<EVirtualTextureCodec>();
                CodecPayloadOffset[layerIndex] = Ar.Read<ushort>();
            }
            BulkData = new FByteBulkData(Ar);
        }
    }
}
