using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkPositioningParams
{
    public EBitsPositioning BitsPositioning { get; }
    public byte? Flags3D { get; }
    public byte? PathMode { get; }
    public int? TransitionTime { get; }
    public List<AkVertex> Vertices { get; }
    public List<AkPlaylistItem> PlaylistItems { get; }
    public List<AkPlaylistRange> PlaylistRanges { get; }

    public AkPositioningParams(FArchive ar)
    {
        BitsPositioning = ar.Read<EBitsPositioning>();

        if (BitsPositioning.IsEmitter() || BitsPositioning.HasFlag(EBitsPositioning.HasListenerRelativeRouting))
        {
            Flags3D = ar.Read<byte>();
        }

        Vertices = new List<AkVertex>();
        PlaylistItems = new List<AkPlaylistItem>();
        PlaylistRanges = new List<AkPlaylistRange>();

        if (BitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent) && BitsPositioning.IsEmitter())
        {
            PathMode = ar.Read<byte>();
            TransitionTime = ar.Read<int>();

            uint numVertices = ar.Read<uint>();
            for (int i = 0; i < numVertices; i++)
            {
                Vertices.Add(new AkVertex(ar));
            }

            uint numPlaylistItems = ar.Read<uint>();
            for (int i = 0; i < numPlaylistItems; i++)
            {
                PlaylistItems.Add(new AkPlaylistItem(ar));
            }

            for (int i = 0; i < numPlaylistItems; i++)
            {
                PlaylistRanges.Add(new AkPlaylistRange(ar));
            }
        }
    }

    public class AkVertex
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public int Duration { get; }

        public AkVertex(FArchive ar)
        {
            X = ar.Read<float>();
            Y = ar.Read<float>();
            Z = ar.Read<float>();
            Duration = ar.Read<int>();
        }
    }

    public class AkPlaylistItem
    {
        public uint VerticesOffset { get; }
        public uint NumVertices { get; }

        public AkPlaylistItem(FArchive ar)
        {
            VerticesOffset = ar.Read<uint>();
            NumVertices = ar.Read<uint>();
        }
    }

    public class AkPlaylistRange
    {
        public float XRange { get; }
        public float YRange { get; }
        public float ZRange { get; }

        public AkPlaylistRange(FArchive ar)
        {
            XRange = ar.Read<float>();
            YRange = ar.Read<float>();
            ZRange = ar.Read<float>();
        }
    }
}
