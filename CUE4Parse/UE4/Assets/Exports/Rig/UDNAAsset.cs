using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class UDNAAsset : UObject
{
    public DNAVersion Version;
    public DNAVersion LayerVersion;
    public Dictionary<string, IRawBase> Sections;
    public Dictionary<string, IRawBase> Layers;
    public Lazy<byte[]>? DNAData;
    public string? DnaFileName;

    private readonly byte[] _signature = "DNA"u8.ToArray();
    private readonly byte[] _eof = "AND"u8.ToArray();
    private long dnaStartPos;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DnaFileName = GetOrDefault<string>(nameof(DnaFileName));

        if (FDNAAssetCustomVersion.Get(Ar) >= FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            dnaStartPos = Ar.Position;
            DNAData = new Lazy<byte[]>(() =>
            {
                Ar.Position = dnaStartPos;
                return Ar.ReadBytes((int) (validPos - dnaStartPos));
            });

            Ar.Position = dnaStartPos;
            var startPos = Ar.Position;
            var endianAr = new FArchiveBigEndian(Ar);

            var signature = endianAr.ReadBytes(3);
            if (!signature.SequenceEqual(_signature))
                throw new InvalidDataException("Invalid file start signature");

            Version = new DNAVersion(endianAr);
#if DEBUG
            Log.Warning("DNAAsset Version {0}", Version.FileVersion.ToString());
#endif
            if (Version.FileVersion < FileVersion.v24)
            {
                var sectionLookupTable = new SectionLookupTable(endianAr);
                var indexTable = new IndexTable(sectionLookupTable, Version);
                if (!ReadLayers(endianAr, indexTable, startPos, out Sections, false))
                    return;

                var eof = endianAr.ReadBytes(3);
                if (!eof.SequenceEqual(_eof))
                    throw new InvalidDataException("Invalid end of file signature");

                if (Ar.Game == EGame.GAME_ArenaBreakoutInfinite)
                    return;
            }
            else
            {
                var indexTable = new IndexTable(endianAr);
                if (!ReadLayers(endianAr, indexTable, startPos, out Sections))
                    return;
            }

            startPos = endianAr.Position;

            signature = endianAr.ReadBytes(3);
            if (!signature.SequenceEqual(_signature))
                throw new InvalidDataException("Invalid layer start signature");

            LayerVersion = new DNAVersion(endianAr);
            var layersIndexTable = new IndexTable(endianAr);
            ReadLayers(endianAr, layersIndexTable, startPos, out Layers);
        }
    }

    private bool ReadLayers(FArchiveBigEndian endianAr, IndexTable indexTable, long startPos, out Dictionary<string, IRawBase> layers, bool validateSizes = true)
    {
        bool result = true;
        layers = new Dictionary<string, IRawBase>(indexTable.Entries.Length);
        foreach (var entry in indexTable.Entries)
        {
            endianAr.Position = startPos + entry.Offset;
            var layerStartPos = endianAr.Position;
            try
            {
                layers[entry.Id] = entry.Id switch
                {
                    "desc" => new RawDescriptor(endianAr),
                    "defn" => new RawDefinition(endianAr),
                    "bhvr" => new RawBehavior(endianAr),
                    "geom" => new RawGeometry(endianAr),
                    "mlbh" => new RawMachineLearnedBehavior(endianAr),
                    "rbfb" => new RawRBFBehavior(endianAr),
                    "rbfe" => new RawRBFBehaviorExt(endianAr),
                    "jbmd" => new RawJointBehaviorMetadata(endianAr),
                    "twsw" => new RawTwistSwingBehavior(endianAr),
                    _ => throw new NotSupportedException($"Type '{entry.Id}' is currently not supported")
                };
            }
            catch (Exception e)
            {
                result = false;
                Log.Error(e, "Failed to read DNA layer '{0}' correctly.", entry.Id);
            }
            finally
            {
                if (validateSizes)
                {
                    var readSize = endianAr.Position - layerStartPos;
                    var remaining = entry.Size - readSize;
                    endianAr.Position = layerStartPos + entry.Size;

                    switch (remaining)
                    {
                        case > 0:
                            Log.Debug("Did not read layer '{0}' correctly. {1} bytes remaining", entry.Id, remaining);
                            break;
                        case < 0:
                            Log.Debug("Did not read layer '{0}' correctly. Read {1} extra bytes", entry.Id, Math.Abs(remaining));
                            break;
                    }
                }
            }
        }

        return result;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Version));
        serializer.Serialize(writer, Version);

        if (Sections.TryGetValue("desc", out var descriptor))
        {
            writer.WritePropertyName("Descriptor");
            serializer.Serialize(writer, descriptor);
        }

        //writer.WritePropertyName("Definition");
        //serializer.Serialize(writer, Definition);

        //writer.WritePropertyName("Behavior");
        //serializer.Serialize(writer, Behavior);

        //writer.WritePropertyName("Geometry");
        //serializer.Serialize(writer, Geometry);

        //writer.WritePropertyName("LayerVersion");
        //serializer.Serialize(writer, LayerVersion);

        //writer.WritePropertyName("IndexTable");
        //serializer.Serialize(writer, IndexTable);

        //writer.WritePropertyName("Layers");
        //serializer.Serialize(writer, Layers);
    }
}
