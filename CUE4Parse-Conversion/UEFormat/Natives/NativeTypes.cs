using System.Runtime.InteropServices;

namespace CUE4Parse_Conversion.UEFormat.Natives;

public enum UEFormatStatus
{
    Ok = 0,
    InvalidArgument = 1,
    OutOfMemory = 2,
    SerializeError = 3,
    TypeMismatch = 4,
}

public enum UEFormatCompression : byte
{
    None = 0,
    Gzip = 1,
    Zstd = 2,
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatBuffer
{
    public IntPtr Data;
    public nuint Size;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatSaveOptions
{
    public IntPtr ObjectName;
    public IntPtr ObjectPath;
    public byte Compression;
    public int CompressionLevel;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatBufferResult
{
    public UEFormatStatus Status;
    public UEFormatBuffer Buffer;
    public IntPtr Error;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatVector
{
    public float X, Y, Z;

    public UEFormatVector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatQuat
{
    public float X, Y, Z, W;

    public UEFormatQuat(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatColor
{
    public byte R, G, B, A;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatMeshUV
{
    public float U, V;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatNormal
{
    public float BinormalSign;
    public UEFormatVector Normal;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatTexCoordEntryDesc
{
    public IntPtr Name;
    public IntPtr Uvs;
    public int UvCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatVertexColorDesc
{
    public IntPtr Name;
    public IntPtr Data;
    public int Count;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatMaterialDesc
{
    public IntPtr MaterialName;
    public IntPtr MaterialPath;
    public int FirstIndex;
    public int NumFaces;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatWeightDesc
{
    public ushort Bone;
    public int VertexIndex;
    public float Weight;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatMorphDataDesc
{
    public UEFormatVector PositionDelta;
    public UEFormatVector TangentZDelta;
    public uint VertexIndex;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatMorphTargetDesc
{
    public IntPtr MorphName;
    public IntPtr MorphData;
    public int MorphDataCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatModelLodDesc
{
    public IntPtr Name;
    public IntPtr Vertices;
    public int VertexCount;
    public IntPtr Normals;
    public int NormalCount;
    public IntPtr Tangents;
    public int TangentCount;
    public IntPtr TextureCoordinates;
    public int TextureCoordinateCount;
    public IntPtr Indices;
    public int IndexCount;
    public IntPtr VertexColors;
    public int VertexColorCount;
    public IntPtr Materials;
    public int MaterialCount;
    public IntPtr Weights;
    public int WeightCount;
    public IntPtr MorphTargets;
    public int MorphTargetCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatBoneDesc
{
    public IntPtr BoneName;
    public int ParentIndex;
    public UEFormatVector Position;
    public UEFormatQuat Orientation;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatSocketDesc
{
    public IntPtr SocketName;
    public IntPtr BoneName;
    public UEFormatVector RelativeLocation;
    public UEFormatQuat RelativeRotation;
    public UEFormatVector RelativeScale;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatVirtualBoneDesc
{
    public IntPtr SourceBoneName;
    public IntPtr TargetBoneName;
    public IntPtr VirtualBoneName;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatModelSkeletonDesc
{
    public IntPtr MetadataPath;
    public IntPtr Bones;
    public int BoneCount;
    public IntPtr Sockets;
    public int SocketCount;
    public IntPtr VirtualBones;
    public int VirtualBoneCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatConvexCollisionDesc
{
    public IntPtr Name;
    public IntPtr VertexData;
    public int VertexCount;
    public IntPtr IndexData;
    public int IndexCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatModelDesc
{
    public IntPtr Lods;
    public int LodCount;
    public IntPtr Skeleton;
    public IntPtr Collisions;
    public int CollisionCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatVectorKeyDesc
{
    public int Frame;
    public UEFormatVector Value;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatQuatKeyDesc
{
    public int Frame;
    public UEFormatQuat Value;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatFloatKeyDesc
{
    public int Frame;
    public float Value;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatAnimMetadataDesc
{
    public int NumFrames;
    public float FramesPerSecond;
    public IntPtr RefPosePath;
    public byte AdditiveAnimType;
    public byte RefPoseType;
    public int RefFrameIndex;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatTrackDesc
{
    public IntPtr BoneName;
    public IntPtr PositionKeys;
    public int PositionKeyCount;
    public IntPtr RotationKeys;
    public int RotationKeyCount;
    public IntPtr ScaleKeys;
    public int ScaleKeyCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatCurveDesc
{
    public IntPtr CurveName;
    public IntPtr Keys;
    public int KeyCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatAnimDesc
{
    public UEFormatAnimMetadataDesc Metadata;
    public IntPtr Tracks;
    public int TrackCount;
    public IntPtr Curves;
    public int CurveCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatPoseKeyDesc
{
    public IntPtr BoneName;
    public UEFormatVector Location;
    public UEFormatQuat Rotation;
    public UEFormatVector Scale;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatPoseCurveInfluenceDesc
{
    public int CurveIndex;
    public float Influence;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatPoseDataDesc
{
    public IntPtr PoseName;
    public IntPtr Keys;
    public int KeyCount;
    public IntPtr Curves;
    public int CurveCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct UEFormatPoseDesc
{
    public IntPtr Poses;
    public int PoseCount;
    public IntPtr CurveNames;
    public int CurveNameCount;
}
