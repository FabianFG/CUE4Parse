using System.ComponentModel;

namespace CUE4Parse_Conversion.Meshes;

public enum ESocketFormat
{
    [Description("Serialize Skeleton Sockets in a Separate Header (SKELSOCK)")]
    Socket,
    [Description("Serialize Skeleton Sockets as Bones")]
    Bone
}