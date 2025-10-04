using System;
using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;
using CUE4Parse.UE4.FMod.Enums;

namespace CUE4Parse.UE4.FMod.Nodes;

public class ModulatorNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid OwnerGuid;
    public readonly int PropertyIndex;
    public readonly EModulatorType Type;
    public readonly EPropertyType PropertyType;
    public readonly EClockSource ClockSource;
    public readonly object? Subnode;

    public ModulatorNode(BinaryReader Ar)
    {
        Ar.ReadUInt16(); // Unknown
        BaseGuid = new FModGuid(Ar);
        OwnerGuid = new FModGuid(Ar);

        PropertyIndex = Ar.ReadInt32();
        Type = (EModulatorType) Ar.ReadInt32();

        if (FModReader.Version < 0x55)
        {
            PropertyType = (EPropertyType) Ar.ReadInt32();
            ClockSource = (EClockSource) Ar.ReadInt32();
        }
        else
        {
            PropertyType = EPropertyType.PropertyType_Normal;
            ClockSource = EClockSource.ClockSource_Local;
        }

        // TODO: something is still off
        switch (Type)
        {
            case EModulatorType.ADSR:
                Subnode = new ADSRModulatorNode(Ar);
                break;
            case EModulatorType.Random:
                Subnode = new RandomModulatorNode(Ar);
                break;
            case EModulatorType.Envelope:
                Subnode = new EnvelopeModulatorNode(Ar);
                break;
            case EModulatorType.LFO:
                Subnode = new LFOModulatorNode(Ar);
                break;
            case EModulatorType.Seek:
                Subnode = new SeekModulatorNode(Ar);
                break;
            case EModulatorType.SpectralSidechain:
                Subnode = new SpectralSidechainModulatorNode(Ar);
                break;
            default:
                Console.WriteLine($"Unhandled modulator type {Type} ({(int) Type}) at stream position {Ar.BaseStream.Position}");
                break;
        }

        if (Subnode is not RandomModulatorNode && Subnode is not LFOModulatorNode)
        {
            Ar.ReadBytes(8); // Unknown
        }
    }
}
