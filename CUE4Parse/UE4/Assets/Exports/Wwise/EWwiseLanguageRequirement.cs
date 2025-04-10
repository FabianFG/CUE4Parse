using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[JsonConverter(typeof(EnumConverter<EWwiseLanguageRequirement>))]
public enum EWwiseLanguageRequirement : byte
{
    IsDefault                                = 0,
    IsOptional                               = 1,
    SFX                                      = 2,
    EWwiseLanguageRequirement_MAX            = 3,
}
