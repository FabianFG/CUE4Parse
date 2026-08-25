using CUE4Parse.GameTypes.FantasyLifeTheGirlWhoStealsTime.Encryption;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.CriWare;

// AWB encryption is not part of standard CRIWARE, which only provides audio samples encryption
// This handles games that implement their own custom AWB encryption
internal static class CriWareAwbDecryption
{
    public static Stream? CreateDecryptingStream(Stream stream, string awbName, EGame game, bool leaveOpen = false) => game switch
    {
        GAME_FantasyLifeTheGirlWhoStealsTime => new FantasyLifeAwbStream(stream, awbName, leaveOpen),
        _ => null
    };

    public static Stream Wrap(Stream stream, string awbName, EGame game)
        => CreateDecryptingStream(stream, awbName, game) ?? stream;
}
