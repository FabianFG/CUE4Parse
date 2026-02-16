using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkFx
{
    public readonly byte FXIndex;
    public readonly uint FXId;
    public readonly byte BitVector;
    public readonly bool IsShareSet; // Version <= 145
    public readonly bool IsRendered; // Version <= 145

    public AkFx(FArchive Ar)
    {
        switch (WwiseVersions.Version)
        {
            case <= 26:
                // No additional fields for version <= 26
                break;
            case <= 145:
                FXIndex = Ar.Read<byte>();
                FXId = Ar.Read<uint>();
                IsShareSet = Ar.Read<byte>() != 0;
                IsRendered = Ar.Read<byte>() != 0;
                break;
            default: // Version > 145
                FXIndex = Ar.Read<byte>();
                FXId = Ar.Read<uint>();
                BitVector = Ar.Read<byte>();
                IsShareSet = (BitVector & (1 << 1)) != 0;
                IsRendered = (BitVector & (1 << 2)) != 0;
                break;
        }
    }
}

public readonly struct AkFxParams
{
    public readonly bool BypassAll;
    public readonly AkFx[] Effects = [];

    public AkFxParams(FArchive Ar)
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

            Effects = Ar.ReadArray(count, () => new AkFx(Ar));
        }
    }
}

public readonly struct AkFxChunk
{
    public readonly byte FxIndex;
    public readonly uint FxId;
    public readonly byte IsShareSet;

    public AkFxChunk(FArchive Ar)
    {
        FxIndex = Ar.Read<byte>();
        FxId = Ar.Read<uint>();
        IsShareSet = Ar.Read<byte>();
    }

    public AkFxChunk(byte fxIndex, uint fxId, byte isShareSet)
    {
        FxIndex = fxIndex;
        FxId = fxId;
        IsShareSet = isShareSet;
    }
}

public class AkFxBus
{
    public readonly byte BitsFxBypass;
    public readonly AkFxChunk[] FxChunks = [];
    public readonly uint FxId0;
    public readonly bool IsShareSet0;

    public AkFxBus(FArchive Ar)
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
        else
        {
            count = Ar.Read<byte>(); // numFX
        }

        bool readFx;
        if (WwiseVersions.Version > 48 && WwiseVersions.Version <= 65)
        {
            readFx = count > 0; // or if is environmental, only possible in versions <= 53, we shouldn't really care about versions < 100
        }
        else
        {
            readFx = count > 0;
        }

        if (readFx)
        {
            if (WwiseVersions.Version > 26)
            {
                BitsFxBypass = Ar.Read<byte>();
            }

            FxChunks = new AkFxChunk[count];
            for (int i = 0; i < count; i++)
            {
                var fxIndex = Ar.Read<byte>();
                var fxId = Ar.Read<uint>();
                var isShareSet = Ar.Read<byte>();
                FxChunks[i] = new AkFxChunk(fxIndex, fxId, isShareSet);

                if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 145)
                {
                    Ar.Read<byte>(); // unused byte
                }
            }
        }

        if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 145)
        {
            FxId0 = Ar.Read<uint>();
            IsShareSet0 = Ar.Read<byte>() != 0;
        }
    }
}
