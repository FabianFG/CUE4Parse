using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;

[StructLayout(LayoutKind.Sequential)]
public struct FMeshContentRange
{
    private const int FirstIndexMaxBits   = 24;
    private const int ContentFlagsMaxBits = 32 - FirstIndexMaxBits;
    private const int FirstIndexBitMask   = (1 << FirstIndexMaxBits) - 1; 
        
    private uint FirstIndex_ContentFlags;
    public uint MeshIDPrefix;

    public EMeshContentFlags ContentFlags => (EMeshContentFlags)((FirstIndex_ContentFlags >> FirstIndexMaxBits) & ((1 << ContentFlagsMaxBits) - 1));
    public uint FirstIndex => FirstIndex_ContentFlags & FirstIndexBitMask;
}

[JsonConverter(typeof(StringEnumConverter))]
[Flags]
public enum EMeshContentFlags : byte
{
    None          = 0,
    GeometryData  = 1 << 0,
    PoseData      = 1 << 1,
    PhysicsData   = 1 << 2,
    MetaData      = 1 << 3,

    LastFlag      = MetaData,
    AllFlags      = (LastFlag << 1) - 1,
}
