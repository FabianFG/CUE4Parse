using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Enums;

// From SDK
[JsonConverter(typeof(StringEnumConverter))]
public enum AkCompanyID : ushort
{
    PluginDevMin = 64,
    PluginDevMax = 255,
    /// <summary>Audiokinetic inc.</summary>
    Audiokinetic = 0,
    /// <summary>Audiokinetic inc.</summary>
    AudiokineticExternal = 1,
    /// <summary>McDSP</summary>
    McDsp = 256,
    /// <summary>WaveArts</summary>
    WaveArts = 257,
    /// <summary>Phonetic Arts</summary>
    PhoneticArts = 258,
    /// <summary>iZotope</summary>
    Izotope = 259,
    /// <summary>Crankcase Audio</summary>
    CrankcaseAudio = 261,
    /// <summary>IOSONO</summary>
    Iosono = 262,
    /// <summary>Auro Technologies</summary>
    AuroTechnologies = 263,
    /// <summary>Dolby</summary>
    Dolby = 264,
    /// <summary>Two Big Ears</summary>
    TwoBigEars = 265,
    /// <summary>Oculus</summary>
    Oculus = 266,
    /// <summary>Blue Ripple Sound</summary>
    BlueRippleSound = 267,
    /// <summary>Enzien Audio</summary>
    Enzien = 268,
    /// <summary>Krotos (Dehumanizer)</summary>
    Krotos = 269,
    /// <summary>Nurulize</summary>
    Nurulize = 270,
    /// <summary>Super Powered</summary>
    SuperPowered = 271,
    /// <summary>Google</summary>
    Google = 272,

    // The following are commented out in the source to avoid redefinition:
    Nvidia = 273,
    Reserved = 274,
    Microsoft = 275,
    Yamaha = 276,

    /// <summary>Visisonics</summary>
    Visisonics = 277
}
