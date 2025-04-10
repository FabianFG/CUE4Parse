using System.IO;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class UDNAAsset : UObject
{
    public DNAVersion Version;
    public SectionLookupTable Sections;
    public RawDescriptor Descriptor;
    public RawDefinition Definition;
    public RawBehavior Behavior;
    public RawGeometry Geometry;

    private readonly byte[] _signature = "DNA"u8.ToArray();
    private readonly byte[] _eof = "AND"u8.ToArray();

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FDNAAssetCustomVersion.Get(Ar) >= FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            var startPos = Ar.Position;
            var endianAr = new FArchiveBigEndian(Ar);

            var signature = Ar.ReadBytes(3);
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

            var eof = Ar.ReadBytes(3);
            if (!eof.SequenceEqual(_eof))
                throw new InvalidDataException("Invalid end of file signature");

            // Layers
            signature = Ar.ReadBytes(3);
            if (!signature.SequenceEqual(_signature))
                throw new InvalidDataException("Invalid layers start signature");

            var fileVersion = new DNAVersion(endianAr);
            endianAr.Position += 4; // seems to have a fixed value of 9
//
            var desc = endianAr.ReadBytes(4);
            var sig = new char[desc.Length];
            for (var i = 0; i < desc.Length; i++)
            {
                sig[i] = (char)desc[i];
            }
//
            var ver = new DNAVersion(endianAr);

            Ar.Position = validPos;
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
    }
}
