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

    public AkPositioningParams(FArchive Ar)
    {
        BitsPositioning = Ar.Read<EBitsPositioning>();

        if (BitsPositioning.IsEmitter() || BitsPositioning.HasFlag(EBitsPositioning.HasListenerRelativeRouting))
        {
            Flags3D = Ar.Read<byte>();
        }

        Vertices = [];
        PlaylistItems = [];
        PlaylistRanges = [];

        if (BitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent) && BitsPositioning.IsEmitter())
        {
            PathMode = Ar.Read<byte>();
            TransitionTime = Ar.Read<int>();

            uint numVertices = Ar.Read<uint>();
            for (int i = 0; i < numVertices; i++)
            {
                Vertices.Add(new AkVertex(Ar));
            }

            uint numPlaylistItems = Ar.Read<uint>();
            for (int i = 0; i < numPlaylistItems; i++)
            {
                PlaylistItems.Add(new AkPlaylistItem(Ar));
            }

            for (int i = 0; i < numPlaylistItems; i++)
            {
                PlaylistRanges.Add(new AkPlaylistRange(Ar));
            }
        }
    }

    public class AkVertex
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public int Duration { get; }

        public AkVertex(FArchive Ar)
        {
            X = Ar.Read<float>();
            Y = Ar.Read<float>();
            Z = Ar.Read<float>();
            Duration = Ar.Read<int>();
        }
    }

    public class AkPlaylistItem
    {
        public uint VerticesOffset { get; }
        public uint NumVertices { get; }

        public AkPlaylistItem(FArchive Ar)
        {
            VerticesOffset = Ar.Read<uint>();
            NumVertices = Ar.Read<uint>();
        }
    }

    public class AkPlaylistRange
    {
        public float XRange { get; }
        public float YRange { get; }
        public float ZRange { get; }

        public AkPlaylistRange(FArchive Ar)
        {
            XRange = Ar.Read<float>();
            YRange = Ar.Read<float>();
            ZRange = Ar.Read<float>();
        }
    }
}
