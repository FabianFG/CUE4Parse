using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[JsonConverter(typeof(EnumConverter<EWwiseSoundBankType>))]
public enum EWwiseSoundBankType : byte
{
    User                                     = 0,
    Event                                    = 30,
    Bus                                      = 31,
    EWwiseSoundBankType_MAX                  = 32,
}
