using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkPositioningParams
{
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EBitsPositioning BitsPositioning;
    public readonly byte? PathMode;
    public readonly int? TransitionTime;
    public readonly List<AkVertex> Vertices;
    public readonly List<AkPlaylistItem> PlaylistItems;
    public readonly List<AkPlaylistRange> PlaylistRanges;

    public AkPositioningParams(FArchive Ar)
    {
        BitsPositioning = Ar.Read<EBitsPositioning>();

        (bool hasPositioning, bool has3dPositioning) = GetPositioningFlags(BitsPositioning);

        if (hasPositioning)
        {
            if (WwiseVersions.Version <= 56)
            {
                Ar.Read<uint>();
                Ar.Read<float>();
                Ar.Read<float>();
            }
        }

        Vertices = [];
        PlaylistItems = [];
        PlaylistRanges = [];

        if (hasPositioning && has3dPositioning)
        {
            EPositioningType positioningType = EPositioningType.Undefined;
            byte flags3d = 0;
            if (WwiseVersions.Version <= 89)
            {
                positioningType = Ar.Read<EPositioningType>();
            }
            else
            {
                flags3d = Ar.Read<byte>();
            }

            if (WwiseVersions.Version <= 89)
            {
                Ar.Read<uint>(); // AttenuationId
                Ar.Read<byte>(); // IsSpatialized
            }
            else if (WwiseVersions.Version <= 129)
            {
                Ar.Read<uint>(); // AttenuationId
            }

            (bool hasAutomation, bool isDynamic) = GetAutomationAndDynamicFlags(positioningType, flags3d, BitsPositioning);

            if (isDynamic)
            {
                Ar.Read<byte>(); // IsDynamic
            }

            if (hasAutomation)
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
    }

    private static (bool hasPositioning, bool has3dPositioning) GetPositioningFlags(EBitsPositioning bitsPositioning)
    {
        bool hasPositioning, has3dPositioning = false;
        switch (WwiseVersions.Version)
        {
            case <= 89:
                hasPositioning = bitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent);
                break;

            case <= 122:
                hasPositioning = bitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent);
                has3dPositioning = bitsPositioning.HasFlag(EBitsPositioning.Is3DPositioningAvailable_122);
                break;

            case <= 129:
                hasPositioning = bitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent);
                has3dPositioning = bitsPositioning.HasFlag(EBitsPositioning.Is3DPositioningAvailable_129);
                break;

            default: // >= 130
                hasPositioning = bitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent);
                has3dPositioning = bitsPositioning.HasFlag(EBitsPositioning.HasListenerRelativeRouting);
                break;
        }
        return (hasPositioning, has3dPositioning);
    }

    private static (bool hasAutomation, bool hasDynamic) GetAutomationAndDynamicFlags(EPositioningType positioningType, int flags3d, EBitsPositioning bitsPositioning)
    {
        bool hasAutomation, isDynamic;
        switch (WwiseVersions.Version)
        {
            case <= 72:
                hasAutomation = positioningType == EPositioningType.UserDefined3D;
                isDynamic = positioningType == EPositioningType.GameDefined3D;
                break;

            case <= 89:
                hasAutomation = positioningType != EPositioningType.GameDefined3D;
                isDynamic = !hasAutomation;
                break;

            case <= 122:
                int e3DPositionType122 = (flags3d >> 0) & 3;
                hasAutomation = e3DPositionType122 != 1;
                isDynamic = false;
                break;

            case <= 126:
                int e3DPositionType126 = (flags3d >> 4) & 1;
                hasAutomation = e3DPositionType126 != 1;
                isDynamic = false;
                break;

            case <= 129:
                int e3DPositionType129 = (flags3d >> 6) & 1;
                hasAutomation = e3DPositionType129 != 1;
                isDynamic = false;
                break;

            default:
                int e3DPositionType130 = ((int)bitsPositioning >> 5) & 3;
                hasAutomation = e3DPositionType130 != 0;
                isDynamic = false;
                break;
        }
        return (hasAutomation, isDynamic);
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
