using Fmod5Sharp.FmodTypes;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Metadata;
using CUE4Parse.UE4.FMod.Nodes;
using CUE4Parse.UE4.FMod.Nodes.Buses;
using CUE4Parse.UE4.FMod.Nodes.Effects;
using CUE4Parse.UE4.FMod.Nodes.Instruments;
using CUE4Parse.UE4.FMod.Nodes.Transitions;
using CUE4Parse.UE4.FMod.Objects;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;
using System.Linq;
using Serilog;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.FMod;

[JsonConverter(typeof(FModConverter))]
public class FModReader
{
    public readonly string BankName;
    public static int Version => FormatInfo.FileVersion;
    public static FFormatInfo FormatInfo;
    public static SoundDataInfo? SoundDataInfo;
    public static byte[]? EncryptionKey;
    public StringTable? StringTable;
    public SoundTable? SoundTable;
    public FBankInfo? BankInfo;
    public FModGuid? PlatformInfo;
    public FHashInfo[] HashData = [];
    public List<FmodSoundBank> SoundBankData = [];

    public readonly Dictionary<FModGuid, EventNode> EventNodes = [];
    public readonly Dictionary<FModGuid, BaseBusNode> BusNodes = [];
    public readonly Dictionary<FModGuid, BaseEffectNode> EffectNodes = [];
    public readonly Dictionary<FModGuid, TimelineNode> TimelineNodes = [];
    public readonly Dictionary<FModGuid, BaseTransitionNode> TransitionNodes = [];
    public readonly Dictionary<FModGuid, BaseInstrumentNode> InstrumentNodes = [];
    public readonly Dictionary<FModGuid, WaveformResourceNode> WavEntries = [];
    public readonly Dictionary<FModGuid, ParameterNode> ParameterNodes = [];
    public readonly Dictionary<FModGuid, ModulatorNode> ModulatorNodes = [];
    public readonly Dictionary<FModGuid, CurveNode> CurveNodes = [];
    public readonly Dictionary<FModGuid, PropertyNode> PropertyNodes = [];
    public readonly Dictionary<FModGuid, MappingNode> MappingNodes = [];
    public readonly Dictionary<FModGuid, ParameterLayoutNode> ParameterLayoutNodes = [];
    public readonly Dictionary<FModGuid, ControllerNode> ControllerNodes = [];
    public readonly Dictionary<FModGuid, SnapshotNode> SnapshotNodes = [];
    public readonly Dictionary<FModGuid, VCANode> VCANodes = [];
    public readonly List<FModGuid> ControllerOwnerNodes = [];

    public FModReader(BinaryReader Ar, string bankName, byte[]? encryptionKey = null)
    {
        BankName = bankName;
        if (encryptionKey != null) EncryptionKey = encryptionKey;
        ParseHeader(Ar);
        ParseNodes(Ar, Ar.BaseStream.Position, Ar.BaseStream.Length);
    }

    private void ParseHeader(BinaryReader Ar)
    {
        if (Ar.BaseStream.Length < 12)
            throw new Exception("File too small to be a valid RIFF header");

        string riff = Encoding.ASCII.GetString(Ar.ReadBytes(4));
        if (riff != "RIFF") throw new Exception("Not a valid RIFF file");

        uint riffSize = Ar.ReadUInt32();
        string fileType = Encoding.ASCII.GetString(Ar.ReadBytes(4));
        if (fileType != "FEV ") throw new Exception("Not a valid FMOD bank");

        long expectedSize = riffSize + 8;
        long actualSize = Ar.BaseStream.Length;

        if (actualSize < expectedSize)
            throw new Exception($"Truncated file: expected {expectedSize} bytes, got {actualSize}");
        else if (actualSize > expectedSize)
            Log.Warning($"File larger than RIFF size (expected {expectedSize}, got {actualSize})");
    }

    private void ParseNodes(BinaryReader Ar, long start, long end)
    {
        Ar.BaseStream.Position = start;

        Stack<FParentContext> parentStack = [];
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

            var nodeId = (ENodeId)rawNodeValue;
            uint nodeSize = Ar.ReadUInt32();
            long nextNode = nodeStart + 8 + nodeSize;

            if (nodeSize == 0)
            {
                Ar.BaseStream.Position = nextNode;
                continue;
            }

            switch (nodeId)
            {
                case ENodeId.CHUNKID_FORMATINFO:
                    FormatInfo = new FFormatInfo(Ar);
                    break;

                case ENodeId.CHUNKID_BANKINFO:
                    BankInfo = new FBankInfo(Ar);
                    break;

                case ENodeId.CHUNKID_STRINGDATA:
                    StringTable = new StringTable(Ar);
                    break;

                case ENodeId.CHUNKID_SOUNDTABLE:
                    SoundTable = new SoundTable(Ar);
                    break;

                case ENodeId.CHUNKID_HASHDATA:
                    HashData = new HashData(Ar);
                    break;

                case ENodeId.CHUNKID_PLATFORM_INFO:
                    PlatformInfo = new FModGuid(Ar);
                    break;

                case ENodeId.CHUNKID_LIST: // List of sub-chunks
                    var listNodeId = (ENodeId)Ar.ReadInt32(); // Not needed; Im using custom structure
                    ParseNodes(Ar, Ar.BaseStream.Position, nextNode);
                    break;

                case ENodeId.CHUNKID_LISTCOUNT:
                    var listCount = Ar.ReadUInt32(); // Not needed; Im using custom structure
                    break;

                case ENodeId.CHUNKID_OUTPUTPORTBODY:
                case ENodeId.CHUNKID_RETURNBUSBODY:
                case ENodeId.CHUNKID_INPUTBUSBODY:
                case ENodeId.CHUNKID_GROUPBUSBODY:
                case ENodeId.CHUNKID_MASTERBUSBODY:
                case ENodeId.CHUNKID_BUS:
                    ParseBusNodes(Ar, nodeId, parentStack);
                    break;

                case ENodeId.CHUNKID_SPECTRALSIDECHAINEFFECT:
                case ENodeId.CHUNKID_BUILTINEFFECTBODY:
                case ENodeId.CHUNKID_SENDEFFECTBODY:
                case ENodeId.CHUNKID_SIDECHAINEFFECT:
                case ENodeId.CHUNKID_PARAMETERIZEDEFFECT:
                case ENodeId.CHUNKID_EFFECTBODY:
                case ENodeId.CHUNKID_PLUGINEFFECTBODY:
                    ParseEffectNodes(Ar, nodeId, parentStack);
                    break;

                case ENodeId.CHUNKID_SCATTERERINSTRUMENTBODY:
                case ENodeId.CHUNKID_MULTIINSTRUMENTBODY:
                case ENodeId.CHUNKID_PLAYLIST:
                case ENodeId.CHUNKID_PROGRAMMERINSTRUMENTBODY:
                case ENodeId.CHUNKID_COMMANDINSTRUMENTBODY:
                case ENodeId.CHUNKID_WAVEFORMINSTRUMENTBODY:
                case ENodeId.CHUNKID_EVENTINSTRUMENTBODY:
                case ENodeId.CHUNKID_SILENCEINSTRUMENTBODY:
                case ENodeId.CHUNKID_INSTRUMENT:
                case ENodeId.CHUNKID_EFFECTINSTRUMENTBODY:
                    ParseInstrumentNodes(Ar, nodeId, parentStack);
                    break;

                case ENodeId.CHUNKID_TRANSITIONREGIONBODY:
                case ENodeId.CHUNKID_TRANSITIONTIMELINE:
                    ParseTransitionNodes(Ar, nodeId, parentStack);
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

                case ENodeId.CHUNKID_MODULATOR:
                case ENodeId.CHUNKID_MODULATORBODY: // Modulator Node
                    {
                        var node = new ModulatorNode(Ar);
                        ModulatorNodes[node.BaseGuid] = node;
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

                case ENodeId.CHUNKID_TIMELINEBODY: // Timeline Node
                    {
                        var node = new TimelineNode(Ar);
                        TimelineNodes[node.BaseGuid] = node;
                    }
                    break;

                case ENodeId.CHUNKID_SNAPSHOTBODY: // Snapshot Node
                    {
                        var node = new SnapshotNode(Ar);
                        SnapshotNodes[node.BaseGuid] = node;
                    }
                    break;

                case ENodeId.CHUNKID_VCA:
                case ENodeId.CHUNKID_VCABODY: // VCA Node
                    {
                        var node = new VCANode(Ar);
                        VCANodes[node.BaseGuid] = node;
                    }
                    break;

                case ENodeId.CHUNKID_CURVE: // Curve Node
                    {
                        var node = new CurveNode(Ar);
                        CurveNodes[node.BaseGuid] = node;
                    }
                    break;

                case ENodeId.CHUNKID_CONTROLLEROWNER: // Controller Owner Node
                    {
                        var node = new ControllerOwnerNode(Ar);
                        ControllerOwnerNodes.AddRange(node.Controllers);
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

                case ENodeId.CHUNKID_SOUNDDATAHEADER: // Sound Data Header
                    SoundDataInfo = new SoundDataInfo(Ar);
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

            // Stop if we already visited a sound node and current node is NOT sound node
            // Not sure why I need to do that but I've seen soundbanks that write duplicated FSB data outside of SND chunk
            // It's important to note there might be multiple SND chunks so we can't just stop after first SND
            if (visitedSoundNode && nodeId != ENodeId.CHUNKID_SOUNDDATA)
                break;

            if (Ar.BaseStream.Position != nextNode)
            {
                if (nodeId is not ENodeId.CHUNKID_LIST)
                    Log.Warning($"Chunk {nodeId} did not parse fully (at {Ar.BaseStream.Position}, should be {nextNode})");

                Ar.BaseStream.Position = nextNode;
            }
        }
    }

    private void ParseBusNodes(BinaryReader Ar, ENodeId nodeId, Stack<FParentContext> parentStack)
    {
        switch (nodeId)
        {
            case ENodeId.CHUNKID_OUTPUTPORTBODY: // Output Port Node
                {
                    var node = new OutputPortNode(Ar);
                    BusNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to bus node
                }
                break;

            case ENodeId.CHUNKID_RETURNBUSBODY: // Return Bus Node
                {
                    var node = new ReturnBusNode(Ar);
                    BusNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to bus node
                }
                break;

            case ENodeId.CHUNKID_INPUTBUSBODY: // Input Bus Node
                {
                    var node = new InputBusNode(Ar);
                    BusNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to bus node
                }
                break;

            case ENodeId.CHUNKID_GROUPBUSBODY: // Group Bus Node
                {
                    var node = new GroupBusNode(Ar);
                    BusNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to bus node
                }
                break;

            case ENodeId.CHUNKID_MASTERBUSBODY: // Master Bus Node
                {
                    var node = new MasterBusNode(Ar);
                    BusNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to bus node
                }
                break;

            case ENodeId.CHUNKID_BUS: // Bus Node
                if (parentStack.TryPeek(out var busParent) &&
                    busParent.NodeId is ENodeId.CHUNKID_INPUTBUSBODY or
                        ENodeId.CHUNKID_GROUPBUSBODY or
                        ENodeId.CHUNKID_MASTERBUSBODY or
                        ENodeId.CHUNKID_RETURNBUSBODY or
                        ENodeId.CHUNKID_OUTPUTPORTBODY)
                {
                    var node = new BusNode(Ar);
                    if (BusNodes.TryGetValue(busParent.Guid, out var baseBus))
                        baseBus.BusBody = node;

                    parentStack.Pop();
                }
                break;
        }
    }

    private void ParseEffectNodes(BinaryReader Ar, ENodeId nodeId, Stack<FParentContext> parentStack)
    {
        switch (nodeId)
        {
            case ENodeId.CHUNKID_BUILTINEFFECTBODY:
                {
                    var node = new BuiltInEffectNode(Ar);
                    EffectNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to parameterized effect node
                    break;
                }

            case ENodeId.CHUNKID_PLUGINEFFECTBODY:
                {
                    var node = new PluginEffectNode(Ar);
                    EffectNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to parameterized effect node
                    break;
                }

            case ENodeId.CHUNKID_PARAMETERIZEDEFFECT:
                {
                    if (parentStack.TryPeek(out var paramEffectParent) &&
                        paramEffectParent.NodeId is ENodeId.CHUNKID_BUILTINEFFECTBODY or
                            ENodeId.CHUNKID_PLUGINEFFECTBODY)
                    {
                        var node = new ParameterizedEffectNode(Ar);
                        if (EffectNodes.TryGetValue(paramEffectParent.Guid, out var builtInEffectNodeObj))
                        {
                            if (builtInEffectNodeObj is BuiltInEffectNode builtInEffectNode)
                            {
                                builtInEffectNode.ParamEffectBody = node;
                            }
                            else if (builtInEffectNodeObj is PluginEffectNode pluginEffectNode)
                            {
                                pluginEffectNode.ParamEffectBody = node;
                            }
                        }

                        parentStack.Pop();
                        parentStack.Push(new FParentContext(nodeId, paramEffectParent.Guid)); // Points to effect node
                    }
                    break;
                }

            case ENodeId.CHUNKID_SPECTRALSIDECHAINEFFECT:
                {
                    var node = new SpectralSideChainEffectNode(Ar);
                    EffectNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to effect node
                    break;
                }

            case ENodeId.CHUNKID_SENDEFFECTBODY:
                {
                    var node = new SendEffectNode(Ar);
                    EffectNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to effect node
                    break;
                }

            case ENodeId.CHUNKID_SIDECHAINEFFECT:
                {
                    var node = new SideChainEffectNode(Ar);
                    EffectNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to effect node
                    break;
                }

            case ENodeId.CHUNKID_EFFECTBODY:
                {
                    if (parentStack.TryPeek(out var effectParent) &&
                        effectParent.NodeId is ENodeId.CHUNKID_SENDEFFECTBODY or
                            ENodeId.CHUNKID_SIDECHAINEFFECT or
                            ENodeId.CHUNKID_SPECTRALSIDECHAINEFFECT or
                            ENodeId.CHUNKID_PARAMETERIZEDEFFECT)
                    {
                        var node = new EffectNode(Ar);
                        if (EffectNodes.TryGetValue(effectParent.Guid, out var effectNode))
                            effectNode.EffectBody = node;

                        parentStack.Pop();
                    }
                    break;
                }
        }
    }

    private void ParseInstrumentNodes(BinaryReader Ar, ENodeId nodeId, Stack<FParentContext> parentStack)
    {
        switch (nodeId)
        {
            case ENodeId.CHUNKID_SCATTERERINSTRUMENTBODY: // Scatterer Instrument Node
                {
                    var node = new ScattererInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to playlist node
                }
                break;

            case ENodeId.CHUNKID_MULTIINSTRUMENTBODY: // Multi Instrument Node
                {
                    var node = new MultiInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to playlist node
                }
                break;

            case ENodeId.CHUNKID_PLAYLIST: // Playlist Node
                if (parentStack.TryPeek(out var parentPlst) &&
                    (parentPlst.NodeId is ENodeId.CHUNKID_SCATTERERINSTRUMENTBODY or ENodeId.CHUNKID_MULTIINSTRUMENTBODY))
                {
                    var node = new PlaylistNode(Ar);
                    if (InstrumentNodes.TryGetValue(parentPlst.Guid, out var parentNodeObj))
                    {
                        if (parentNodeObj is ScattererInstrumentNode scatterer)
                        {
                            scatterer.PlaylistBody = node;
                        }
                        else if (parentNodeObj is MultiInstrumentNode multi)
                        {
                            multi.PlaylistBody = node;
                        }
                    }
                    parentStack.Pop();
                    parentStack.Push(new FParentContext(nodeId, parentPlst.Guid)); // Points to instrument node
                }
                break;

            case ENodeId.CHUNKID_PROGRAMMERINSTRUMENTBODY: // Programmer Instrument Node
                {
                    var node = new ProgrammerInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to instrument node
                }
                break;

            case ENodeId.CHUNKID_COMMANDINSTRUMENTBODY: // Command Instrument Node
                {
                    var node = new CommandInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to instrument node
                }
                break;

            case ENodeId.CHUNKID_WAVEFORMINSTRUMENTBODY: // Waveform Instrument Node
                {
                    var node = new WaveformInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to instrument node
                }
                break;

            case ENodeId.CHUNKID_EVENTINSTRUMENTBODY: // Event Instrument Node
                {
                    var node = new EventInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to instrument node
                }
                break;

            case ENodeId.CHUNKID_SILENCEINSTRUMENTBODY: // Silence Instrument Node
                {
                    var node = new SilenceInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to instrument node
                }
                break;

            case ENodeId.CHUNKID_EFFECTINSTRUMENTBODY: // Effect Instrument Node
                {
                    var node = new EffectInstrumentNode(Ar);
                    InstrumentNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to instrument node
                }
                break;

            case ENodeId.CHUNKID_INSTRUMENT: // Instrument Node
                if (parentStack.TryPeek(out var parentInst) &&
                    (parentInst.NodeId is ENodeId.CHUNKID_PROGRAMMERINSTRUMENTBODY or
                        ENodeId.CHUNKID_COMMANDINSTRUMENTBODY or
                        ENodeId.CHUNKID_WAVEFORMINSTRUMENTBODY or
                        ENodeId.CHUNKID_EVENTINSTRUMENTBODY or
                        ENodeId.CHUNKID_SILENCEINSTRUMENTBODY or
                        ENodeId.CHUNKID_PLAYLIST or
                        ENodeId.CHUNKID_EFFECTINSTRUMENTBODY))
                {
                    var node = new InstrumentNode(Ar);
                    if (InstrumentNodes.TryGetValue(parentInst.Guid, out var instNode))
                        instNode.InstrumentBody = node;

                    parentStack.Pop();
                }
                break;
        }
    }

    private void ParseTransitionNodes(BinaryReader Ar, ENodeId nodeId, Stack<FParentContext> parentStack)
    {
        switch (nodeId)
        {
            case ENodeId.CHUNKID_TRANSITIONREGIONBODY: // Transition Region Node
                {
                    var node = new TransitionRegionNode(Ar);
                    TransitionNodes[node.BaseGuid] = node;
                    parentStack.Push(new FParentContext(nodeId, node.BaseGuid)); // Points to transition timeline node
                }
                break;

            case ENodeId.CHUNKID_TRANSITIONTIMELINE: // Transition Timeline Node
                if (parentStack.TryPeek(out var transParent) &&
                    transParent.NodeId is ENodeId.CHUNKID_TRANSITIONREGIONBODY)
                {
                    var node = new TransitionTimelineNode(Ar);
                    if (TransitionNodes.TryGetValue(transParent.Guid, out var transNode))
                        transNode.TransitionBody = node;

                    parentStack.Pop();
                }
                break;
        }
    }

    public List<FmodSample> ExtractTracks()
        => [.. SoundBankData.SelectMany(bank => bank.Samples)];

    public FModGuid GetBankGuid()
        => BankInfo?.BaseGuid ?? new FModGuid();

    public void Merge(FModReader src)
    {
        foreach (var kv in src.EventNodes)
            EventNodes[kv.Key] = kv.Value;
        foreach (var kv in src.BusNodes)
            BusNodes[kv.Key] = kv.Value;
        foreach (var kv in src.EffectNodes)
            EffectNodes[kv.Key] = kv.Value;
        foreach (var kv in src.TimelineNodes)
            TimelineNodes[kv.Key] = kv.Value;
        foreach (var kv in src.TransitionNodes)
            TransitionNodes[kv.Key] = kv.Value;
        foreach (var kv in src.InstrumentNodes)
            InstrumentNodes[kv.Key] = kv.Value;
        foreach (var kv in src.WavEntries)
            WavEntries[kv.Key] = kv.Value;
        foreach (var kv in src.ParameterNodes)
            ParameterNodes[kv.Key] = kv.Value;
        foreach (var kv in src.ModulatorNodes)
            ModulatorNodes[kv.Key] = kv.Value;
        foreach (var kv in src.CurveNodes)
            CurveNodes[kv.Key] = kv.Value;
        foreach (var kv in src.PropertyNodes)
            PropertyNodes[kv.Key] = kv.Value;
        foreach (var kv in src.MappingNodes)
            MappingNodes[kv.Key] = kv.Value;
        foreach (var kv in src.ParameterLayoutNodes)
            ParameterLayoutNodes[kv.Key] = kv.Value;
        foreach (var kv in src.ControllerNodes)
            ControllerNodes[kv.Key] = kv.Value;
        foreach (var kv in src.SnapshotNodes)
            SnapshotNodes[kv.Key] = kv.Value;
        foreach (var kv in src.VCANodes)
            VCANodes[kv.Key] = kv.Value;

        SoundBankData.AddRange(src.SoundBankData);
        ControllerOwnerNodes.AddRange(src.ControllerOwnerNodes);
        if (src.HashData is { Length: > 0 })
            HashData = [.. HashData, .. src.HashData];
    }

    #region Global Readers

    public static uint ReadX16(BinaryReader Ar)
    {
        short signedLow = Ar.ReadInt16();
        ushort low = (ushort)signedLow;
        uint value = low;

        if ((low & 0x8000) != 0)
        {
            ushort high = Ar.ReadUInt16();
            value &= 0x7FFFu;
            value |= ((uint)high << 15);
        }

        return value;
    }

    public static string ReadString(BinaryReader Ar)
    {
        uint length = ReadX16(Ar);

        if (length <= 0) return string.Empty;

        var bytes = Ar.ReadBytes((int)length);

        return Encoding.UTF8.GetString(bytes);
    }

    public static T[] ReadVersionedElemListImp<T>(BinaryReader Ar, Func<BinaryReader, T>? readElem = null)
    {
        uint raw = ReadX16(Ar);
        int count = (int)(raw >> 1);

        if (count <= 0) return [];

        var result = new T[count];

        for (int i = 0; i < count; i++)
        {
            _ = Ar.ReadUInt16(); // Payload size
            if (readElem != null)
            {
                result[i] = readElem(Ar);
            }
            else
            {
                result[i] = (T)Activator.CreateInstance(typeof(T), Ar)!;
            }
        }

        return result;
    }

    public static T[] ReadElemListImp<T>(BinaryReader Ar, Func<BinaryReader, T>? readElem = null)
    {
        uint raw = ReadX16(Ar);
        int count = (int) (raw >> 1);

        if (count <= 0)
            return [];

        var result = new T[count];

        _ = Ar.ReadUInt16(); // Payload size
        for (int i = 0; i < count; i++)
        {
            if (readElem != null)
            {
                result[i] = readElem(Ar);
            }
            else
            {
                result[i] = (T) Activator.CreateInstance(typeof(T), Ar)!;
            }
        }

        return result;
    }

    public static FUInt24[] ReadSimpleArray24(BinaryReader Ar)
    {
        uint count = ReadX16(Ar);

        if (count == 0) return [];

        int totalBytes = checked((int)count * 3); // 3 bytes per entry
        byte[] raw = Ar.ReadBytes(totalBytes);

        if (raw.Length != totalBytes) throw new EndOfStreamException($"Expected {totalBytes} bytes, got {raw.Length}");

        var arr = new FUInt24[count];
        int o = 0;
        for (int i = 0; i < count; i++)
        {
            uint v = (uint)(raw[o] | (raw[o + 1] << 8) | (raw[o + 2] << 16));
            o += 3;
            arr[i] = new FUInt24(v);
        }
        return arr;
    }

    #endregion
}
