using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum EAkEntryType : int
{
    EntryMarker,
    SameTime,
    RandomMarker,
    RandomUserMarker,
    LastExitTime,
};
