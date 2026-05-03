using System.ComponentModel;

namespace CUE4Parse_Conversion.V2.Options;

public enum ESocketFormat
{
    [Description("Export Bone Sockets as a Custom Chunk")]
    Socket,
    [Description("Export Bone Sockets as Bones")]
    Bone,
    [Description("Don't Export Bone Sockets")]
    None
}
