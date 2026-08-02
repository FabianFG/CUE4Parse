using System.ComponentModel;

namespace CUE4Parse_Conversion.Options;

public enum ESocketFormat
{
    [Description("Export Bone Sockets as a Custom Chunk")]
    Socket,
    [Description("Export Bone Sockets as Bones")]
    Bone,
    [Description("Don't Export Bone Sockets")]
    None
}
