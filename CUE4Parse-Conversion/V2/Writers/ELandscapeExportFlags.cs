using System;

namespace CUE4Parse_Conversion.Landscape;

[Flags]
public enum ELandscapeExportFlags
{
    Heightmap = 1 << 0,
    Weightmap = 1 << 1,
    Mesh = 1 << 3,
    All = Heightmap | Weightmap | Mesh
}
