using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[JsonConverter(typeof(EnumConverter<EWwiseEventDestroyOptions>))]
public enum EWwiseEventDestroyOptions : byte
{
    StopEventOnDestroy                       = 0,
    WaitForEventEnd                          = 1,
    EWwiseEventDestroyOptions_MAX            = 2,
}
