using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Readers;

public struct AkFX
{
    public byte FXIndex;
    public uint FXID;
    public byte BitVector;
}

public struct AkFXParams
{
    public bool OverrideFX;
    public bool BypassAll;
    public List<AkFX> Effects;
}

public static class AkFXParamsReader
{
    public static AkFXParams ReadFXChain(this FArchive Ar)
    {
        byte bOverrideFx = Ar.Read<byte>();
        byte uNumFx = Ar.Read<byte>();

        bool overrideFX = bOverrideFx != 0 && uNumFx != 0;
        bool bypassAll = false;
        var effects = new List<AkFX>();

        if (overrideFX)
        {
            bypassAll = Ar.Read<byte>() != 0;

            for (int i = 0; i < uNumFx; i++)
            {
                byte fxIndex = Ar.Read<byte>();
                uint fxID = Ar.Read<uint>();
                byte bitVector = Ar.Read<byte>();

                effects.Add(new AkFX
                {
                    FXIndex = fxIndex,
                    FXID = fxID,
                    BitVector = bitVector
                });
            }
        }

        return new AkFXParams
        {
            OverrideFX = overrideFX,
            BypassAll = bypassAll,
            Effects = effects
        };
    }
}
