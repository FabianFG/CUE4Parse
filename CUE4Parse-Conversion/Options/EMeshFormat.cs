using System.ComponentModel;

namespace CUE4Parse_Conversion.Options;

public enum EMeshFormat
{
    [Description("ActorX (psk / pskx)")]
    ActorX,
    [Description("glTF 2.0 (binary)")]
    Gltf2,
    // 2 is free, use it // nothing is free
    [Description("Alembic (abc)")]
    Alembic,
    [Description("UEFormat (uemodel)")]
    UEFormat,
    [Description("Universal Scene Description (usda)")]
    USD
}
