using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
[JsonConverter(typeof(StringEnumConverter))]
public enum EAkAdvSettingsFlags : byte
{
    None = 0,
    KillNewest = 1 << 0,
    UseVirtualBehavior = 1 << 1,
    IgnoreParentMaxNumInst = 1 << 3,
    IsVVoicesOptOverrideParent = 1 << 4,

    IsMaxNumInstOverrideParent = IgnoreParentMaxNumInst
}
