using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Animation.ACL;

namespace CUE4Parse.ACL
{
    public class DecompressionContext
    {
        internal IntPtr _handle;

        public DecompressionContext()
        {
            _handle = nDecompContextDefault_Create();
        }

        public CompressedTracks? GetCompressedTracks()
        {
            var compressedTracks = nDecompContextDefault_GetCompressedTracks(_handle);
            return compressedTracks != IntPtr.Zero ? new CompressedTracks(compressedTracks) : null;
        }

        public bool Initialize(CompressedTracks tracks) => nDecompContextDefault_Initialize(_handle, tracks._handle);

        public void Seek(float sampleTime, SampleRoundingPolicy roundingPolicy) => nDecompContextDefault_Seek(_handle, sampleTime, roundingPolicy);

        public void DecompressTracks(FTransform[] atoms, FAtomIndices[] trackToAtomsMap)
        {
            var writer = nCreateOutputWriter(atoms, trackToAtomsMap);
            nDecompContextDefault_DecompressTracks(_handle, writer);
        }

        public void DecompressTrack(int trackIndex, out FTransform atom)
        {
            atom = default;
            var writer = nCreateOutputTrackWriter(ref atom);
            nDecompContextDefault_DecompressTrack(_handle, trackIndex, writer);
        }

        [DllImport(ACLNative.LIB_NAME)]
        private static extern IntPtr nDecompContextDefault_Create();

        [DllImport(ACLNative.LIB_NAME)]
        private static extern IntPtr nDecompContextDefault_GetCompressedTracks(IntPtr handle);

        [DllImport(ACLNative.LIB_NAME)]
        private static extern bool nDecompContextDefault_Initialize(IntPtr handle, IntPtr tracks);

        [DllImport(ACLNative.LIB_NAME)]
        private static extern void nDecompContextDefault_Seek(IntPtr handle, float sampleTime, SampleRoundingPolicy roundingPolicy);

        [DllImport(ACLNative.LIB_NAME)]
        private static extern IntPtr nCreateOutputWriter(FTransform[] atoms, FAtomIndices[] trackToAtomsMap);

        [DllImport(ACLNative.LIB_NAME)]
        private static extern IntPtr nCreateOutputTrackWriter(ref FTransform atom);

        [DllImport(ACLNative.LIB_NAME)]
        private static extern void nDecompContextDefault_DecompressTracks(IntPtr handle, IntPtr writer);

        [DllImport(ACLNative.LIB_NAME)]
        private static extern void nDecompContextDefault_DecompressTrack(IntPtr handle, int trackIndex, IntPtr writer);
    }
}