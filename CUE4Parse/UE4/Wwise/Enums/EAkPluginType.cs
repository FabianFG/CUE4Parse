using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum EAkPluginType : int
{
    None = 0x0,
    Codec = 0x1,
    Source = 0x2,
    Effect = 0x3,
    MotionDevice = 0x4, // 125 <=
    MotionSource = 0x5, // 125 <=
    Mixer = 0x6,
    Sink = 0x7,
    GlobalExtension = 0x8,
    Metadata = 0x9,
    Last = 0xA,
    Mask = 0xF,
};
