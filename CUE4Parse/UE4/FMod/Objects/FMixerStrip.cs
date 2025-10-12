using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FMixerStrip
{
    public readonly float Volume;
    public readonly float Pitch;
    public readonly FModGuid[] VCAs = [];

    public FMixerStrip(BinaryReader Ar)
    {
        Ar.ReadUInt16(); // Payload size
        Volume = Ar.ReadSingle();
        Pitch = Ar.ReadSingle();

        if (FModReader.Version >= 0x6c)
        {
            VCAs = FModReader.ReadElemListImp<FModGuid>(Ar);
        }
    }
}
