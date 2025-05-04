using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkFX
{
    public byte FXIndex { get; set; }
    public uint FXID { get; set; }
    public byte BitVector { get; set; }
    public bool IsShareSet { get; set; } // Version <= 145
    public bool IsRendered { get; set; } // Version <= 145

    public AkFX(FArchive Ar)
    {
        if (WwiseVersions.WwiseVersion <= 26)
        {
            // No additional fields for version <= 26
        }
        else if (WwiseVersions.WwiseVersion <= 145)
        {
            FXIndex = Ar.Read<byte>();
            FXID = Ar.Read<uint>();
            IsShareSet = Ar.Read<byte>() != 0;
            IsRendered = Ar.Read<byte>() != 0;
        }
        else
        {
            FXIndex = Ar.Read<byte>();
            FXID = Ar.Read<uint>();
            BitVector = Ar.Read<byte>();
            IsShareSet = (BitVector & (1 << 1)) != 0;
            IsRendered = (BitVector & (1 << 2)) != 0;
        }
    }
}

public class AkFXParams
{
    public bool OverrideFX { get; set; }
    public bool BypassAll { get; set; }
    public List<AkFX> Effects { get; set; }

    public AkFXParams(FArchive Ar)
    {
        OverrideFX = Ar.Read<byte>() != 0;

        int count;
        if (WwiseVersions.WwiseVersion <= 26)
        {
            count = Ar.Read<uint>() != 0 ? 1 : 0; // uNumFx (flag check for version <= 26)
        }
        else
        {
            count = Ar.Read<byte>(); // uNumFx
        }

        Effects = new List<AkFX>();

        if (count > 0)
        {
            if (WwiseVersions.WwiseVersion <= 26)
            {
                // No additional fields for version <= 26
            }
            else if (WwiseVersions.WwiseVersion <= 145)
            {
                BypassAll = Ar.Read<byte>() != 0;
            }
            else
            {
                BypassAll = Ar.Read<byte>() != 0;
            }

            for (int i = 0; i < count; i++)
            {
                Effects.Add(new AkFX(Ar));
            }
        }
    }
}
