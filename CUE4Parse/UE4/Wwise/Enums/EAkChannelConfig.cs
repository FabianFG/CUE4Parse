using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Enums;

// WPropertyEnums::BusChannelConfig
[JsonConverter(typeof(StringEnumConverter))]
public enum EAkChannelConfig : uint
{
    SameasAudioDevice = 0,
    SameasMainMix = 3584,
    SameasPassThroughMix = 3840,
    AudioObjects = 768,
    Audio_1_0 = 16641,
    Audio_2_0 = 12546,
    Audio_2_1 = 45315,
    Audio_3_0 = 28931,
    Audio_4_0 = 6304004,
    Audio_5_1 = 6353158,
    Audio_7_1 = 6549768,
    Audio_5_1_2 = 90239240,
    Audio_5_1_4 = 761327882,
    Audio_7_1_2 = 90435850,
    Audio_7_1_4 = 761524492,
    Ambisonics1storder = 516,
    Ambisonics2ndorder = 521,
    Ambisonics3rdorder = 528,
    Ambisonics4thorder = 537,
    Ambisonics5thorder = 548,
    Auro10_1 = 769716491,
    Auro11_1 = 803270924,
    Auro13_1 = 803467534,
    LFE = 33025,
}
