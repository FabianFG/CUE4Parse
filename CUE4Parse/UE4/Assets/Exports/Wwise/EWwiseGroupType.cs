using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[JsonConverter(typeof(EnumConverter<EWwiseGroupType>))]
public enum EWwiseGroupType : byte
{
    Switch                                   = 0,
    State                                    = 1,
    Unknown                                  = 255,
    // EWwiseGroupType_MAX                      = 256,
}
