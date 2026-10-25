using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public sealed class DnaBinaryDocument
{
    public DNAVersion Version;
    public Dictionary<string, IRawBase> Layers = new();
    public long ByteLength;
}

public static class DnaBinaryParser
{
    private static readonly byte[] Signature = "DNA"u8.ToArray();
    private static readonly byte[] EofMarker = "AND"u8.ToArray();

    public static bool LooksLikeDna(FArchive Ar)
    {
        if (Ar.Position + 3 > Ar.Length)
            return false;
        var pos = Ar.Position;
        var sig = Ar.ReadBytes(3);
        Ar.Position = pos;
        return sig.SequenceEqual(Signature);
    }

    public static DnaBinaryDocument Parse(FArchiveBigEndian endianAr, long startPos, bool validateSizes = true)
    {
        endianAr.Position = startPos;
        var signature = endianAr.ReadBytes(3);
        if (!signature.SequenceEqual(Signature))
            throw new InvalidDataException("Invalid file start signature");

        var version = new DNAVersion(endianAr);
        Dictionary<string, IRawBase> layers;

        if (version.FileVersion < FileVersion.v23)
        {
            var sectionLookupTable = new SectionLookupTable(endianAr);
            var indexTable = new IndexTable(sectionLookupTable, version);
            ReadLayers(endianAr, version.FileVersion, indexTable, startPos, out layers, validateSizes: false);
            var eof = endianAr.ReadBytes(3);
            if (!eof.SequenceEqual(EofMarker))
                throw new InvalidDataException("Invalid end of file signature");
        }
        else
        {
            var indexTable = new IndexTable(endianAr);
            ReadLayers(endianAr, version.FileVersion, indexTable, startPos, out layers, validateSizes);

            var dnaEnd = startPos;
            foreach (var entry in indexTable.Entries)
                dnaEnd = Math.Max(dnaEnd, startPos + entry.Offset + entry.Size);

            endianAr.Position = dnaEnd;
            return new DnaBinaryDocument
            {
                Version = version,
                Layers = layers,
                ByteLength = dnaEnd - startPos
            };
        }

        return new DnaBinaryDocument
        {
            Version = version,
            Layers = layers,
            ByteLength = endianAr.Position - startPos
        };
    }

    public static bool ReadLayers(FArchiveBigEndian endianAr, FileVersion fileVersion, IndexTable indexTable, long startPos,
        out Dictionary<string, IRawBase> layers, bool validateSizes = true)
    {
        var result = true;
        layers = new Dictionary<string, IRawBase>(indexTable.Entries.Length);
        foreach (var entry in indexTable.Entries)
        {
            endianAr.Position = startPos + entry.Offset;
            var layerStartPos = endianAr.Position;
            try
            {
                var layerId = NormalizeLayerId(entry.Id);
                layers[layerId] = layerId switch
                {
                    "desc" => new RawDescriptor(endianAr),
                    "defn" => new RawDefinition(endianAr),
                    "dsce" => new RawDescriptorExt(endianAr, fileVersion),
                    "bhvr" => new RawBehavior(endianAr),
                    "geom" => new RawGeometry(endianAr),
                    "mlbe" => new RawMachineLearnedBehaviorExt(endianAr),
                    "mlbh" => new RawMachineLearnedBehavior(endianAr),
                    "rbfb" => new RawRBFBehavior(endianAr),
                    "rbfe" => new RawRBFBehaviorExt(endianAr),
                    "jbmd" => new RawJointBehaviorMetadata(endianAr),
                    "twsw" => new RawTwistSwingBehavior(endianAr),
                    _ => throw new NotSupportedException($"Type '{layerId}' is currently not supported")
                };
            }
            catch (Exception e)
            {
                result = false;
                Log.Error(e, "Failed to read DNA layer '{0}' correctly.", entry.Id.Trim('\0'));
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

    public static string NormalizeLayerId(string id) => id.TrimEnd('\0');
}
