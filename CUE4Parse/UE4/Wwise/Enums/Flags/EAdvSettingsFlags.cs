using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
[JsonConverter(typeof(StringEnumConverter))]
public enum EAdvSettingsFlags : byte
{
    None = 0,
    KillNewest = 1 << 0,
    UseVirtualBehavior = 1 << 1,
    UnknownFlag2 = 1 << 2, // v154
    IgnoreParentMaxNumInst = 1 << 3,
    VVoicesOptOverrideParent = 1 << 4
}
