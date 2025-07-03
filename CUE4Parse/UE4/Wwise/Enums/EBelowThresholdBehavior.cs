namespace CUE4Parse.UE4.Wwise.Enums;

public enum EBelowThresholdBehavior : byte
{
    ContinueToPlay = 0x0,
    KillVoice = 0x1,
    SetAsVirtualVoice = 0x2,
    KillIfOneShotElseVirtual = 0x3
}
