using System.ComponentModel;

namespace CUE4Parse_Conversion.Meshes;

public enum ESocketFormat
{
    [Description("Export Bone Sockets in a Separate Header (SKELSOCK)")]
    Socket,
    [Description("Export Bone Sockets as Bones")]
    Bone,
    [Description("Don't Export Bone Sockets")]
    None
}