using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse.UE4.FMod.Nodes;
using CUE4Parse.UE4.FMod.Objects;
using Fmod5Sharp.FmodTypes;
using CUE4Parse.UE4.FMod.Enums;
using Serilog;

namespace CUE4Parse.UE4.FMod;

public class FModReader
{
    public static int Version => FormatInfo.FileVersion;
    public static FormatInfo FormatInfo { get; private set; } = null!;
    public static SoundDataHeaderNode? SoundDataHeader;
    public StringDataNode? StringData;
    public BankInfoNode? BankInfo;

    public readonly Dictionary<FModGuid, EventNode> EventNodes = [];
    public readonly Dictionary<FModGuid, TimelineNode> TimelineNodes = [];
    public readonly Dictionary<FModGuid, PlaylistNode> PlaylistNodes = [];
    public readonly Dictionary<FModGuid, InstrumentNode> InstrumentNodes = [];
    public readonly Dictionary<FModGuid, WaveformResourceNode> WavEntries = [];
    public readonly Dictionary<FModGuid, ScattererInstrumentNode> ScattererInstrumentNodes = [];
    public readonly Dictionary<FModGuid, ParameterNode> ParameterNodes = [];
    public readonly Dictionary<FModGuid, ModulatorNode> ModulatorNodes = [];
    public readonly Dictionary<FModGuid, CurveNode> CurveNodes = [];
    public readonly Dictionary<FModGuid, PropertyNode> PropertyNodes = [];
    public readonly Dictionary<FModGuid, MappingNode> MappingNodes = [];
    public readonly Dictionary<FModGuid, ParameterLayoutNode> ParameterLayoutNodes = [];
    public readonly Dictionary<FModGuid, ControllerNode> ControllerNodes = [];
    public readonly Dictionary<FModGuid, FModGuid> WaveformInstrumentNodes = [];

    public List<FmodSoundBank> SoundBankData = [];
    public FHashData[] HashData = [];

    public FModReader(BinaryReader Ar)
    {
        ParseHeader(Ar);
        ParseNodes(Ar, Ar.BaseStream.Position, Ar.BaseStream.Length);
    }

    private static void ParseHeader(BinaryReader Ar)
    {
        if (Ar.BaseStream.Length < 12)
            throw new Exception("File too small to be a valid RIFF header");

        string riff = Encoding.ASCII.GetString(Ar.ReadBytes(4));
        if (riff != "RIFF")
            throw new Exception("Not a valid RIFF file");

        int riffSize = Ar.ReadInt32();
        string fileType = Encoding.ASCII.GetString(Ar.ReadBytes(4));
        if (fileType != "FEV ")
            throw new Exception("Not a valid FMOD bank");

        long expectedSize = riffSize + 8;
        long actualSize = Ar.BaseStream.Length;

        if (actualSize < expectedSize)
            throw new Exception($"Truncated file: expected {expectedSize} bytes, got {actualSize}");
        else if (actualSize > expectedSize)
            Log.Warning($"Warning: file larger than RIFF size (expected {expectedSize}, got {actualSize})");
    }

    private void ParseNodes(BinaryReader Ar, long start, long end)
    {
        Ar.BaseStream.Position = start;

        FModGuid? playlistParentGuid = null;
        bool visitedSoundNode = false;
        int soundDataIndex = 0;

        while (Ar.BaseStream.Position + 8 <= end)
        {
            long nodeStart = Ar.BaseStream.Position;
            var rawNodeValue = Ar.ReadInt32();

            // Shift to correct position if end of the node starts with null terminator
            // (usually it's end of a list but not always)
            if ((rawNodeValue & 0xFF) == 0x00)
            {
                nodeStart = Ar.BaseStream.Position - 3;
                Ar.BaseStream.Position -= 3;
                rawNodeValue = Ar.ReadInt32();
            }

            var nodeId = (ENodeId) rawNodeValue;

            int nodeSize = Ar.ReadInt32();
            if (nodeId is ENodeId.CHUNKID_BUILTINEFFECTBODY)
                nodeSize++;

            long nextNode = nodeStart + 8 + nodeSize;

            if (nodeSize != 0)
            {
                switch (nodeId)
                {
                    case ENodeId.CHUNKID_FORMATINFO:
                        FormatInfo = new FormatInfo(Ar);
                        break;

                    case ENodeId.CHUNKID_BANKINFO:
                        BankInfo = new BankInfoNode(Ar);
                        break;

                    case ENodeId.CHUNKID_LIST: // List of sub-chunks
                        var listNodeId = (ENodeId) Ar.ReadInt32();
                        ParseNodes(Ar, Ar.BaseStream.Position, nextNode);
                        break;

                    case ENodeId.CHUNKID_LISTCOUNT:
                        var listCount = Ar.ReadUInt32();
                        break;

                    case ENodeId.CHUNKID_PROPERTY: // Property Node
                        {
                            var node = new PropertyNode(Ar);
                            PropertyNodes[node.MappingGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_EVENTBODY: // Audio Event Node
                        {
                            var node = new EventNode(Ar);
                            EventNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_MODULATORBODY: // Modulator Node
                        {
                            var node = new ModulatorNode(Ar);
                            ModulatorNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_STRINGDATA: // String Data Node
                        {
                            StringData = new StringDataNode(Ar);
                        }
                        break;

                    case ENodeId.CHUNKID_PARAMETERBODY: // Parameter Node
                        {
                            var node = new ParameterNode(Ar);
                            ParameterNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_PARAMETERLAYOUTBODY: // Parameter Layout Node
                        {
                            var node = new ParameterLayoutNode(Ar);
                            ParameterLayoutNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_WAVEFORMRESOURCE: // Single WAV Node
                        {
                            var node = new WaveformResourceNode(Ar);
                            WavEntries[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_SCATTERERINSTRUMENTBODY: // Scatterer Instrument Node
                        {
                            var node = new ScattererInstrumentNode(Ar);
                            playlistParentGuid = node.BaseGuid; // Also points to playlist which always comes as a next node
                            ScattererInstrumentNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_MULTIINSTRUMENTBODY: // Multi Instrument Node
                        playlistParentGuid = new FModGuid(Ar); // Multi simply points to playlist which always comes as a next node
                        break;

                    case ENodeId.CHUNKID_WAVEFORMINSTRUMENTBODY: // Waveform Instrument Node
                        {
                            var node = new WaveformInstrumentNode(Ar);
                            WaveformInstrumentNodes[node.BaseGuid] = node.WaveformResourceGuid;
                        }
                        break;

                    case ENodeId.CHUNKID_INSTRUMENT: // Instrument Node
                        {
                            var node = new InstrumentNode(Ar);
                            InstrumentNodes[node.TimelineGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_TIMELINEBODY: // Timeline Node
                        {
                            var node = new TimelineNode(Ar);
                            TimelineNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_PLAYLIST: // Playlist Node
                        if (playlistParentGuid != null)
                        {
                            PlaylistNodes[playlistParentGuid.Value] = new PlaylistNode(Ar);
                            playlistParentGuid = null;
                        }
                        break;

                    case ENodeId.CHUNKID_BUILTINEFFECTBODY:
                        {
                            Ar.ReadByte(); // Unknown byte that isn't a part of BEFF body
                        }
                        break;

                    case ENodeId.CHUNKID_HASHDATA: // Hash Node
                        {
                            HashData = new HashDataNode(Ar).HashData;
                        }
                        break;

                    case ENodeId.CHUNKID_CURVE: // Curve Node
                        {
                            var node = new CurveNode(Ar);
                            CurveNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_CONTROLLER: // Controller Node
                        {
                            var node = new ControllerNode(Ar);
                            ControllerNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_MAPPING: // Mapping Node
                        {
                            var node = new MappingNode(Ar);
                            MappingNodes[node.BaseGuid] = node;
                        }
                        break;

                    case ENodeId.CHUNKID_SOUNDDATAHEADER: // Sound Data Header Node
                        {
                            SoundDataHeader = new SoundDataHeaderNode(Ar);
                        }
                        break;

                    case ENodeId.CHUNKID_SOUNDDATA: // Sound Data Node
                        {
                            var node = new SoundDataNode(Ar, nodeStart, nodeSize, soundDataIndex);
                            visitedSoundNode = true;
                            soundDataIndex++;
                            if (node.SoundBank != null)
                            {
                                SoundBankData.Add(node.SoundBank);
                            }
                        }
                        break;

                    default:
                        Log.Warning($"Unknown chunk {nodeId} at {nodeStart}, size={nodeSize}, skipped");
                        break;
                }
            }

            // Stop if we already visited a sound node and current node is NOT sound node
            // Not sure why I need to do that but I've seen soundbanks that write duplicated FSB data outside of SND chunk
            // It's important to note there might be multiple SND chunks so we can't just stop after first SND
            if (visitedSoundNode && nodeId != ENodeId.CHUNKID_SOUNDDATA)
                break;

            if (Ar.BaseStream.Position != nextNode)
            {
#if DEBUG
                Log.Warning($"Warning: chunk {nodeId} did not parse fully (at {Ar.BaseStream.Position}, should be {nextNode})");
#endif
                Ar.BaseStream.Position = nextNode;
            }
            //else
            //{
            //    Console.WriteLine($"Chunk {nodeId} parsed successfully ({nodeStart} -> {nextNode})");
            //}
        }
    }

    #region Global Readers

    public static uint ReadX16(BinaryReader Ar)
    {
        short signedLow = Ar.ReadInt16();
        ushort low = (ushort) signedLow;
        uint value = low;
        if ((low & 0x8000) != 0)
        {
            ushort high = Ar.ReadUInt16();
            value &= 0x7FFFu;
            value |= ((uint) high << 15);
        }
        return value;
    }

    public static string ReadSerializedString(BinaryReader Ar)
    {
        uint length = ReadX16(Ar);

        if (length <= 0)
            return string.Empty;

        var bytes = Ar.ReadBytes((int) length);

        return Encoding.UTF8.GetString(bytes);
    }

    public static T[] ReadElemListImp<T>(BinaryReader Ar, int? size = null)
    {

        uint raw = ReadX16(Ar);
        int count = (int) (raw >> 1);
        bool hasSizePrefix = (raw & 1) != 0; // Element list "size prefix" is a single size value used for the element payloads

        if (count <= 0)
            return [];

        var result = new T[count];

        ushort elementSize = 0;
        if (hasSizePrefix)
        {
            elementSize = Ar.ReadUInt16();
        }

        for (int i = 0; i < count; i++)
        {
            // I don't know what do in case where the element size is different than expected so I'll just skip it
            // (probably we're just reading it wrong and this should actually throw)
            if (hasSizePrefix && size != null && elementSize != size)
            {
                Ar.BaseStream.Position += elementSize;
#if DEBUG
                Log.Debug($"Warning: '{typeof(T).Name}' element size {elementSize} does not match expected {size}, skipping");
#endif
            }
            else
            {
                result[i] = (T) Activator.CreateInstance(typeof(T), Ar)!;
            }
        }

        return result;
    }

    #endregion
}
