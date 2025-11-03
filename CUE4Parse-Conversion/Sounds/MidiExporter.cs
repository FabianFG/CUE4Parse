using System;
using System.Text;
using CUE4Parse.UE4.Assets.Exports.Harmonix;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Sounds;

public static class MidiExporter
{
    public static byte[] Export(this UMidiFile midiFile)
    {
        using var writer = new FArchiveWriter(true);
        var tracks = midiFile.TheMidiData.Tracks;

        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'h');
        writer.Write((byte)'d');
        
        writer.Write((uint)6);
        writer.Write((ushort)1);
        writer.Write((ushort)tracks.Length);
        writer.Write((ushort)midiFile.TheMidiData.TicksPerQuarterNote);

        foreach (var track in tracks)
        {
            writer.Write((byte)'M');
            writer.Write((byte)'T');
            writer.Write((byte)'r');
            writer.Write((byte)'k');

            var bytes = GetTrackBytes(track);
            writer.Write((uint)bytes.Length);
            writer.Write(bytes);
        }

        return writer.GetBuffer();
    }

    private static byte[] GetTrackBytes(FMidiTrack track)
    {
        using var writer = new FArchiveWriter();

        var currentTick = 0;
        foreach (var evebt in track.Events)
        {
            var message = evebt.Message;
            if (message.Type == EType.Runtime)
                continue;
            
            ProcessTick(writer, evebt.Tick, ref currentTick);

            switch (message.Type)
            {
                case EType.Std:
                {
                    var status = message.Status;
                    var data1 = message.Data1;
                    var data2 = message.Data2;

                    writer.Write(status);
                    
                    switch (status & MidiConstants.GMessageTypeMask)
                    {
                        case MidiConstants.GNoteOn: // 3-byte
                        case MidiConstants.GNoteOff:
                        case MidiConstants.GControl:
                        case MidiConstants.GPitch:
                        case MidiConstants.GPolyPres:
                        {
                            writer.Write(data1);
                            writer.Write(data2);
                            break;
                        }
                        case MidiConstants.GProgram:
                        case MidiConstants.GChanPres:
                        {
                            writer.Write(data1);
                            break;
                        }
                    }
                    
                    break;
                }
                case EType.Tempo:
                {
                    var tempo = message.MicsPerQuarterNoteH << 16 | message.MicsPerQuarterNoteL;
                    if (tempo > 0xFFFFFF)
                        throw new ArgumentOutOfRangeException();
                    
                    writer.Write(MidiConstants.GFile_Meta);
                    writer.Write(MidiConstants.GMeta_Tempo);
                    
                    writer.Write((byte)0x03);
                    writer.Write((byte)((tempo >> 16) & 0xFF));
                    writer.Write((byte)((tempo >> 8) & 0xFF));
                    writer.Write((byte)(tempo & 0xFF));
                    
                    break;
                }
                case EType.TimeSig:
                {
                    var numerator = message.Numerator;
                    var denominator = message.Denominator;

                    if (numerator <= 0)
                        throw new ArgumentOutOfRangeException(nameof(numerator), numerator, "Numerator must be greater than zero.");                    
                    
                    if (denominator <= 0)
                        throw new ArgumentOutOfRangeException(nameof(denominator), denominator, "Denominator must be greater than zero.");                    
                    
                    writer.Write(MidiConstants.GFile_Meta);
                    writer.Write(MidiConstants.GMeta_TimeSig);
                    writer.Write((byte)0x04);
                    writer.Write(numerator);
                    
                    writer.Write((byte)Math.Log2(denominator));
                    
                    writer.Write((byte)24);
                    writer.Write((byte)8);
                    
                    break;
                }
                case EType.Text:
                {
                    var str = track.GetTextAtIndex(message.TextIndex);
                    var type = message.TextType;

                    if (type is < 0x01 or > 0x07)
                        throw new ArgumentOutOfRangeException();
                    
                    writer.Write(MidiConstants.GFile_Meta);
                    writer.Write(type);

                    var utf8Bytes = Encoding.UTF8.GetBytes(str);
                    var length = utf8Bytes.Length;
                    
                    WriteVarLenNumber(writer, length);
                    writer.Write(utf8Bytes);
                    
                    break;
                }
                default:
                    throw new NotSupportedException($"Message of Type: '{message.Type}' is currently not supported.");
            }
        }
        
        ProcessTick(writer, currentTick, ref currentTick);
        
        writer.Write(MidiConstants.GFile_Meta);
        writer.Write(MidiConstants.GMeta_EndOfTrack);
        writer.Write((byte)0);
        
        return writer.GetBuffer();
    }

    private static void ProcessTick(FArchiveWriter writer, int tick, ref int currentTick)
    {
        if (tick < currentTick)
            throw new InvalidOperationException($"Tick '{tick}' cannot be less than the current tick '{currentTick}'.");    
        
        WriteVarLenNumber(writer, tick - currentTick);
        currentTick = tick;
    }

    private static void WriteVarLenNumber(FArchiveWriter writer, int value)
    {
        if (value >= 0x0fffffff)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Variable value should be less than 0x0fffffff");

        var length = 1;
        var workingValue = value;
        var buffer = workingValue & 0x7f;

        while ((workingValue >>= 7) != 0 && length < 4)
        {
            buffer <<= 8;
            buffer |= (workingValue & 0x7f) | 0x80;
            length++;
        }

        for (var i = 0; i < length; i++)
        {
            var bytee = buffer & 0xff;
            writer.Write((byte)bytee);

            buffer >>= 8;
        }
    }
    
    private static class MidiConstants
    {
        public const byte GFile_Meta       = 0xff;
        public const byte GMessageTypeMask = 0xf0;

        public const byte GNoteOff  = 0x80; // Note Off
        public const byte GNoteOn   = 0x90; // Note On
        public const byte GPolyPres = 0xa0; // Polyphonic Key Pressure 
        public const byte GControl  = 0xb0; // Control Change
        public const byte GProgram  = 0xc0; // Program Change (2 bytes)
        public const byte GChanPres = 0xd0; // Channel Pressure (2 bytes)
        public const byte GPitch    = 0xe0; // Pitch Wheel Change
        
        public const byte GMeta_EndOfTrack = 0x2f;
        public const byte GMeta_Tempo      = 0x51;
        public const byte GMeta_TimeSig    = 0x58;
    }
}