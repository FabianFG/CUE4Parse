using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkFX
{
    public byte FXIndex { get; set; }
    public uint FXId { get; set; }
    public byte BitVector { get; set; }
    public bool IsShareSet { get; set; } // Version <= 145
    public bool IsRendered { get; set; } // Version <= 145

    public AkFX(FArchive Ar)
    {
        if (WwiseVersions.Version <= 26)
        {
            // No additional fields for version <= 26
        }
        else if (WwiseVersions.Version <= 145)
        {
            FXIndex = Ar.Read<byte>();
            FXId = Ar.Read<uint>();
            IsShareSet = Ar.Read<byte>() != 0;
            IsRendered = Ar.Read<byte>() != 0;
        }
        else
        {
            FXIndex = Ar.Read<byte>();
            FXId = Ar.Read<uint>();
            BitVector = Ar.Read<byte>();
            IsShareSet = (BitVector & (1 << 1)) != 0;
            IsRendered = (BitVector & (1 << 2)) != 0;
        }
    }
}

public class AkFXParams
{
    public bool BypassAll { get; set; }
    public List<AkFX> Effects { get; set; }

    public AkFXParams(FArchive Ar)
    {
        int count;
        if (WwiseVersions.Version <= 26)
        {
            count = Ar.Read<uint>() != 0 ? 1 : 0; // uNumFx (flag check for version <= 26)
        }
        else
        {
            count = Ar.Read<byte>(); // uNumFx
        }

        Effects = [];
        if (count > 0)
        {
            if (WwiseVersions.Version <= 26)
            {
                // No additional fields for version <= 26
            }
            else if (WwiseVersions.Version <= 145)
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

public class AkFXChunk
{
    public byte FXIndex { get; set; }
    public uint FXId { get; set; }
    public byte IsShareSet { get; set; }

    public AkFXChunk(FArchive Ar)
    {
        FXIndex = Ar.Read<byte>();
        FXId = Ar.Read<uint>();
        IsShareSet = Ar.Read<byte>();
    }

    public AkFXChunk(byte fxIndex, uint fxId, byte isShareSet)
    {
        FXIndex = fxIndex;
        FXId = fxId;
        IsShareSet = isShareSet;
    }
}

public class AkFXBus
{
    public byte BitsFXBypass { get; set; }
    public List<AkFXChunk> FXChunks { get; set; } = [];
    public uint FXId0 { get; set; }
    public bool IsShareSet0 { get; set; }

    public AkFXBus(FArchive Ar)
    {
        int count = 0;
        if (WwiseVersions.Version <= 26)
        {
            var numFX = Ar.Read<uint>();
            if (numFX != 0)
            {
                count = 1;
            }
        }
        else if (WwiseVersions.Version <= 145)
        {
            count = Ar.Read<byte>(); // numFX
        }
        else
        {
            count = 0;
        }

        bool readFX = false;
        if (WwiseVersions.Version > 48 && WwiseVersions.Version <= 65)
        {
            readFX = count > 0; // TODO: or if is enviromental, only possible in versions <= 53
        }
        else
        {
            readFX = count > 0;
        }

        if (readFX)
        {
            if (WwiseVersions.Version > 26)
            {
                BitsFXBypass = Ar.Read<byte>();
            }

            for (int i = 0; i < count; i++)
            {
                var fxIndex = Ar.Read<byte>();
                var fxId = Ar.Read<uint>();
                var isShareSet = Ar.Read<byte>();
                FXChunks.Add(new AkFXChunk(fxIndex, fxId, isShareSet));
                Ar.Read<byte>(); // unused byte
            }
        }

        if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 145)
        {
            FXId0 = Ar.Read<uint>();
            IsShareSet0 = Ar.Read<byte>() != 0;
        }
    }
}
