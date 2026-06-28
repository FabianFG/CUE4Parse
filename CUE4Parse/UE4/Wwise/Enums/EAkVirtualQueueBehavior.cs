using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum EAkVirtualQueueBehavior : byte
{
    FromBeginning = 0x0,
    FromElapsedTime = 0x1,
    Resume = 0x2
}
