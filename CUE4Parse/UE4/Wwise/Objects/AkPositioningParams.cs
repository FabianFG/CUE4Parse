using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkPositioningParams
{
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EBitsPositioningFlags BitsPositioning;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkPathMode PathMode;
    public readonly bool IsLooping;
    public readonly int TransitionTime;
    public readonly AkPathVertex[] Vertices;
    public readonly AkPathListItemOffset[] PlaylistItems;
    public readonly AkPathListItem[] PlaylistRanges;

    public AkPositioningParams(FWwiseArchive Ar)
    {
        BitsPositioning = Ar.Read<EBitsPositioningFlags>();

        var has3dPositioning = false;
        var hasPositioning = BitsPositioning.HasFlag(EBitsPositioningFlags.PositioningInfoOverrideParent);
        if (hasPositioning)
        {
            if (Ar.Version <= 56)
            {
                Ar.Read<uint>();
                Ar.Read<float>();
                Ar.Read<float>();
            }

            switch (Ar.Version)
            {
                case <= 72:
                    has3dPositioning = Ar.ReadBool(); // cbIs3DPositioningAvailable
                    if (!has3dPositioning)
                        Ar.Read<byte>(); // bIsPannerEnabled
                    break;
                case <= 89:
                {
                    bool has2dPositioning = Ar.ReadBool(); // cbIs2DPositioningAvailable
                    has3dPositioning = Ar.ReadBool();      // cbIs3DPositioningAvailable
                    if (has2dPositioning)
                        Ar.Read<byte>(); // bPositioningEnablePanner
                    break;
                }
                case <= 122:
                    has3dPositioning = BitsPositioning.HasFlag(EBitsPositioningFlags.Is3DPositioningAvailable_122);
                    break;
                case <= 129:
                    has3dPositioning = BitsPositioning.HasFlag(EBitsPositioningFlags.Is3DPositioningAvailable_129);
                    break;
                default:
                    has3dPositioning = BitsPositioning.HasFlag(EBitsPositioningFlags.HasListenerRelativeRouting);
                    break;
            }
        }

        Vertices = [];
        PlaylistItems = [];
        PlaylistRanges = [];
        if (hasPositioning && has3dPositioning)
        {
            EPositioningType positioningType = EPositioningType.Undefined;
            byte flags3d = 0;
            switch (Ar.Version)
            {
                case <= 89:
                    positioningType = Ar.Read<EPositioningType>();
                    break;
                default:
                    flags3d = Ar.Read<byte>();
                    break;
            }

            switch (Ar.Version)
            {
                case <= 89:
                    Ar.Read<uint>(); // AttenuationId
                    Ar.Read<byte>(); // IsSpatialized
                    break;
                case <= 129:
                    Ar.Read<uint>(); // AttenuationId
                    break;
            }

            (bool hasAutomation, bool isDynamic) = GetAutomationAndDynamicFlags(Ar, positioningType, flags3d, BitsPositioning);

            if (isDynamic)
            {
                Ar.Read<byte>(); // IsDynamic
            }

            if (hasAutomation)
            {
                if (Ar.Version <= 89)
                {
                    PathMode = (EAkPathMode) Ar.Read<uint>();
                    IsLooping = Ar.ReadBool();
                    TransitionTime = Ar.Read<int>();
                    if (Ar.Version > 36)
                        Ar.Read<byte>(); // bFollowOrientation
                }
                else
                {
                    PathMode = Ar.Read<EAkPathMode>();
                    TransitionTime = Ar.Read<int>();
                }

                Vertices = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkPathVertex(Ar));

                var numPlaylistItems = Ar.Read<uint>();
                PlaylistItems = Ar.ReadArray((int) numPlaylistItems, () => new AkPathListItemOffset(Ar));
                PlaylistRanges = Ar.Version switch
                {
                    <= 36 => [],
                    _ => Ar.ReadArray((int) numPlaylistItems, () => new AkPathListItem(Ar)),
                };
            }
        }
    }

    private static (bool hasAutomation, bool hasDynamic) GetAutomationAndDynamicFlags(FWwiseArchive Ar, EPositioningType positioningType, int flags3d, EBitsPositioningFlags bitsPositioning)
    {
        bool hasAutomation, isDynamic;
        switch (Ar.Version)
        {
            case <= 72:
                hasAutomation = positioningType == EPositioningType.UserDefined3D;
                isDynamic = positioningType == EPositioningType.GameDefined3D;
                break;
            case <= 89:
            {
                int eType = ((int) positioningType) & 3;

                hasAutomation = eType != 1;
                isDynamic = !hasAutomation;
                break;
            }
            case <= 122:
            {
                int e3DPositionType122 = flags3d & 3;

                hasAutomation = e3DPositionType122 != 1;
                isDynamic = false;
                break;
            }
            case <= 126:
            {
                int e3DPositionType126 = (flags3d >> 4) & 1;

                hasAutomation = e3DPositionType126 != 1;
                isDynamic = false;
                break;
            }
            case <= 129:
            {
                int e3DPositionType129 = (flags3d >> 6) & 1;

                hasAutomation = e3DPositionType129 != 1;
                isDynamic = false;
                break;
            }
            default:
            {
                int e3DPositionType130 = ((int) bitsPositioning >> 5) & 3;

                hasAutomation = e3DPositionType130 != 0;
                isDynamic = false;
                break;
            }
        }

        return (hasAutomation, isDynamic);
    }

    public readonly struct AkPathVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly int Duration;

        public AkPathVertex(FWwiseArchive Ar)
        {
            X = Ar.Read<float>();
            Y = Ar.Read<float>();
            Z = Ar.Read<float>();
            Duration = Ar.Read<int>();
        }
    }

    public readonly struct AkPathListItemOffset
    {
        public readonly uint VerticesOffset;
        public readonly uint NumVertices;

        public AkPathListItemOffset(FWwiseArchive Ar)
        {
            VerticesOffset = Ar.Read<uint>();
            NumVertices = Ar.Read<uint>();
        }
    }

    public readonly struct AkPathListItem
    {
        public readonly float XRange;
        public readonly float YRange;
        public readonly float ZRange;

        public AkPathListItem(FWwiseArchive Ar)
        {
            XRange = Ar.Read<float>();
            YRange = Ar.Read<float>();
            ZRange = Ar.Version > 89 ? Ar.Read<float>() : 0;
        }
    }
}
