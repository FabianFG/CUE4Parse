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
    public SectionLookupTable Sections;
    public RawDescriptor Descriptor;
    public RawDefinition Definition;
    public RawBehavior Behavior;
    public RawGeometry Geometry;
    public DNAVersion LayerVersion;
    public IndexTable IndexTable;
    public Dictionary<string, IRawBase> Layers;

    private readonly byte[] _signature = "DNA"u8.ToArray();
    private readonly byte[] _eof = "AND"u8.ToArray();

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FDNAAssetCustomVersion.Get(Ar) >= FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            var startPos = Ar.Position;
            var endianAr = new FArchiveBigEndian(Ar);

            var signature = endianAr.ReadBytes(3);
            if (!signature.SequenceEqual(_signature))
                throw new InvalidDataException("Invalid file start signature");

            Version = new DNAVersion(endianAr);
            Sections = new SectionLookupTable(endianAr);

            endianAr.Position = startPos + Sections.Descriptor;
            Descriptor = new RawDescriptor(endianAr);

            endianAr.Position = startPos + Sections.Definition;
            Definition = new RawDefinition(endianAr);

            endianAr.Position = startPos + Sections.Behaviour;
            Behavior = new RawBehavior(endianAr, Sections, startPos);

            endianAr.Position = startPos + Sections.Geometry;
            Geometry = new RawGeometry(endianAr);

            var eof = endianAr.ReadBytes(3);
            if (!eof.SequenceEqual(_eof))
                throw new InvalidDataException("Invalid end of file signature");

            startPos = endianAr.Position;

            signature = endianAr.ReadBytes(3);
            if (!signature.SequenceEqual(_signature))
                throw new InvalidDataException("Invalid layer start signature");

            LayerVersion = new DNAVersion(endianAr);
            IndexTable = new IndexTable(endianAr);

            Layers = [];
            foreach (var entry in IndexTable.Entries)
            {
                endianAr.Position = startPos + entry.Offset;
                var layerStartPos = endianAr.Position;

                Layers[entry.Id] = entry.Id switch
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

                var readSize = endianAr.Position - layerStartPos;
                var remaining = entry.Size - readSize;

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

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Version");
        serializer.Serialize(writer, Version);

        writer.WritePropertyName("Descriptor");
        serializer.Serialize(writer, Descriptor);

        writer.WritePropertyName("Definition");
        serializer.Serialize(writer, Definition);

        writer.WritePropertyName("Behavior");
        serializer.Serialize(writer, Behavior);

        writer.WritePropertyName("Geometry");
        serializer.Serialize(writer, Geometry);

        writer.WritePropertyName("LayerVersion");
        serializer.Serialize(writer, LayerVersion);

        writer.WritePropertyName("IndexTable");
        serializer.Serialize(writer, IndexTable);

        writer.WritePropertyName("Layers");
        serializer.Serialize(writer, Layers);
    }
}
