using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

[JsonConverter(typeof(AkSwitchParamsConverter))]
public class AkSwitchParams
{
    public uint NodeID { get; private set; }
    public bool IsFirstOnly { get; private set; }
    public bool ContinuePlayback { get; private set; }
    public EOnSwitchMode OnSwitchMode { get; private set; }
    public int FadeOutTime { get; private set; }
    public int FadeInTime { get; private set; }

    public AkSwitchParams(FArchive Ar)
    {
        NodeID = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion <= 89)
        {
            IsFirstOnly = Ar.Read<byte>() != 0;
            ContinuePlayback = Ar.Read<byte>() != 0;
            var onSwitchModeBitVector = Ar.Read<byte>();
            OnSwitchMode = (EOnSwitchMode) (onSwitchModeBitVector & 0b00000001);
        }
        else if (WwiseVersions.WwiseVersion <= 150)
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
