using System.Runtime.CompilerServices;

namespace CUE4Parse.ACL
{
    public struct RawBufferHeader
    {
        public uint Size;
        public uint Hash;
    }

    public struct TracksHeader
    {
        public uint Tag;
        public ushort Version;
        public byte AlgorithmType;
        public byte TrackType;
        public uint NumTracks;
        public uint NumSamples;
        public float SampleRate;
        public uint MiscPacked;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetHasScale() => (MiscPacked & 1) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDefaultScale() => (int) (MiscPacked >> 1) & 1;
    }
}
