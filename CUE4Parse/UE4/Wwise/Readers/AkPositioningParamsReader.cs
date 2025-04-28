using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public struct AkVertex
{
    public float X;
    public float Y;
    public float Z;
    public int Duration;
}

public struct AkPlaylistItem
{
    public uint VerticesOffset;
    public uint NumVertices;
}

public struct AkPlaylistRange
{
    public float XRange;
    public float YRange;
    public float ZRange;
}

public struct AkPositioningParams
{
    public EBitsPositioning BitsPositioning;
    public byte? Flags3D;
    public byte? PathMode;
    public int? TransitionTime;
    public List<AkVertex> Vertices;
    public List<AkPlaylistItem> PlaylistItems;
    public List<AkPlaylistRange> PlaylistRanges;
}

public static class AkPositioningParamsReader
{
    public static AkPositioningParams ReadPositioning(this FArchive Ar)
    {
        var bitsPositioning = Ar.Read<EBitsPositioning>();

        byte? flags3D = null;
        if (bitsPositioning.IsEmitter() || bitsPositioning.HasFlag(EBitsPositioning.HasListenerRelativeRouting))
        {
            flags3D = Ar.Read<byte>();
        }

        byte? pathMode = null;
        int? transitionTime = null;
        var vertices = new List<AkVertex>();
        var playlistItems = new List<AkPlaylistItem>();
        var playlistRanges = new List<AkPlaylistRange>();

        if (bitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent) && bitsPositioning.IsEmitter())
        {
            pathMode = Ar.Read<byte>();
            transitionTime = Ar.Read<int>();

            uint numVertices = Ar.Read<uint>();
            for (int i = 0; i < numVertices; i++)
            {
                vertices.Add(new AkVertex
                {
                    X = Ar.Read<float>(),
                    Y = Ar.Read<float>(),
                    Z = Ar.Read<float>(),
                    Duration = Ar.Read<int>()
                });
            }

            uint numPlaylistItems = Ar.Read<uint>();
            for (int i = 0; i < numPlaylistItems; i++)
            {
                playlistItems.Add(new AkPlaylistItem
                {
                    VerticesOffset = Ar.Read<uint>(),
                    NumVertices = Ar.Read<uint>()
                });
            }

            for (int i = 0; i < numPlaylistItems; i++)
            {
                playlistRanges.Add(new AkPlaylistRange
                {
                    XRange = Ar.Read<float>(),
                    YRange = Ar.Read<float>(),
                    ZRange = Ar.Read<float>()
                });
            }
        }

        return new AkPositioningParams
        {
            BitsPositioning = bitsPositioning,
            Flags3D = flags3D,
            PathMode = pathMode,
            TransitionTime = transitionTime,
            Vertices = vertices,
            PlaylistItems = playlistItems,
            PlaylistRanges = playlistRanges
        };
    }
}
