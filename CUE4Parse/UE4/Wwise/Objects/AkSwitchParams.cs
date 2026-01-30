using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkSwitchParams
{
    public readonly uint NodeId;
    public readonly bool IsFirstOnly;
    public readonly bool ContinuePlayback;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EOnSwitchMode OnSwitchMode;
    public readonly int FadeOutTime;
    public readonly int FadeInTime;

    public AkSwitchParams(FArchive Ar)
    {
        NodeId = Ar.Read<uint>();

        if (WwiseVersions.Version <= 89)
        {
            IsFirstOnly = Ar.Read<byte>() != 0;
            ContinuePlayback = Ar.Read<byte>() != 0;
            var onSwitchModeBitVector = Ar.Read<byte>();
            OnSwitchMode = (EOnSwitchMode) (onSwitchModeBitVector & 0b00000001);
        }
        else if (WwiseVersions.Version <= 150)
        {
            var bitVector = Ar.Read<byte>();
            IsFirstOnly = (bitVector & 0b00000001) != 0;
            ContinuePlayback = (bitVector & 0b00000010) != 0;
            var onSwitchModeBitVector = Ar.Read<byte>();
            OnSwitchMode = (EOnSwitchMode) (onSwitchModeBitVector & 0b00000001);
        }
        else
        {
            var bitVector = Ar.Read<byte>();
            IsFirstOnly = (bitVector & 0b00000001) != 0;
            ContinuePlayback = (bitVector & 0b00000010) != 0;
        }

        FadeOutTime = Ar.Read<int>();
        FadeInTime = Ar.Read<int>();
    }
}
