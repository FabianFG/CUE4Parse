using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Enums.Flags;

namespace CUE4Parse.UE4.Wwise.Objects;

public sealed class AkAdvSettingsParams
{
    public EAkAdvSettingsFlags AdvSettingsFlags { get; }
    public EAkVirtualQueueBehavior VirtualQueueBehavior { get; }
    public ushort MaxNumInstance { get; }
    public bool IsGlobalLimit { get; }
    public EAkBelowThresholdBehavior BelowThresholdBehavior { get; }
    public EAkHdrEnvelopeFlags HdrEnvelopeFlags { get; }

    public AkAdvSettingsParams(FWwiseArchive Ar)
    {
        switch (Ar.Version)
        {
            case <= 36:
                VirtualQueueBehavior = (EAkVirtualQueueBehavior) Ar.Read<uint>();
                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.KillNewest;

                MaxNumInstance = Ar.Read<ushort>();
                BelowThresholdBehavior = (EAkBelowThresholdBehavior) Ar.Read<uint>();

                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.IsMaxNumInstOverrideParent;
                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.IsVVoicesOptOverrideParent;
                break;
            case <= 53:
                VirtualQueueBehavior = (EAkVirtualQueueBehavior) Ar.Read<byte>();
                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.KillNewest;

                MaxNumInstance = Ar.Read<ushort>();
                BelowThresholdBehavior = (EAkBelowThresholdBehavior) Ar.Read<byte>();

                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.IsMaxNumInstOverrideParent;
                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.IsVVoicesOptOverrideParent;
                break;
            case <= 89:
                VirtualQueueBehavior = (EAkVirtualQueueBehavior) Ar.Read<byte>();
                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.KillNewest;
                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.UseVirtualBehavior;

                MaxNumInstance = Ar.Read<ushort>();
                IsGlobalLimit = Ar.Read<byte>() != 0;
                BelowThresholdBehavior = (EAkBelowThresholdBehavior) Ar.Read<byte>();

                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.IsMaxNumInstOverrideParent;
                if (Ar.Read<byte>() != 0)
                    AdvSettingsFlags |= EAkAdvSettingsFlags.IsVVoicesOptOverrideParent;

                if (Ar.Version > 72)
                {
                    if (Ar.Read<byte>() != 0)
                        HdrEnvelopeFlags |= EAkHdrEnvelopeFlags.OverrideHdrEnvelope;
                    if (Ar.Read<byte>() != 0)
                        HdrEnvelopeFlags |= EAkHdrEnvelopeFlags.OverrideAnalysis;
                    if (Ar.Read<byte>() != 0)
                        HdrEnvelopeFlags |= EAkHdrEnvelopeFlags.NormalizeLoudness;
                    if (Ar.Read<byte>() != 0)
                        HdrEnvelopeFlags |= EAkHdrEnvelopeFlags.EnableEnvelope;
                }
                break;
            default:
                AdvSettingsFlags = (EAkAdvSettingsFlags) Ar.Read<byte>();
                VirtualQueueBehavior = (EAkVirtualQueueBehavior) Ar.Read<byte>();
                MaxNumInstance = Ar.Read<ushort>();
                BelowThresholdBehavior = (EAkBelowThresholdBehavior) Ar.Read<byte>();
                HdrEnvelopeFlags = (EAkHdrEnvelopeFlags) Ar.Read<byte>();
                break;
        }
    }
}
