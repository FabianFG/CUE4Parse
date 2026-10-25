using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Assets.Exports.Rig.RigLogic;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public abstract class UDNAObject : UObject, IDnaAsset
{
    public DNAVersion? Version { get; protected set; }
    public Dictionary<string, IRawBase>? Layers { get; protected set; }
    public Lazy<byte[]>? DNAData { get; protected set; }
    public string? DnaFileName { get; protected set; }
    public RigLogicSnapshot? Snapshot { get; protected set; }

    public int GetRawControlCount() => GetRawControlNames().Length;

    public string[] GetRawControlNames() => DnaAssetQueries.GetRawControlNames(Layers);

    public string GetRawControlName(int index)
    {
        var names = GetRawControlNames();
        return index >= names.Length
            ? throw new IndexOutOfRangeException($"Index {index} is greater than total raw control count")
            : names[index];
    }

    public int GetJointCount() => GetJointNames().Length;

    public string[] GetJointNames() => DnaAssetQueries.GetJointNames(Layers);

    public string GetJointName(int index)
    {
        var names = GetJointNames();
        return index >= names.Length
            ? throw new IndexOutOfRangeException($"Index {index} is greater than total joint count")
            : names[index];
    }

    public RawBehavior? GetBehavior() => DnaAssetQueries.GetBehavior(Layers);

    protected void ApplyDocument(DnaBinaryDocument doc, byte[] dnaBytes)
    {
        Version = doc.Version;
        Layers = doc.Layers;
        DNAData = new Lazy<byte[]>(() => dnaBytes);
    }

    private static (DnaBinaryDocument Doc, byte[] Bytes) ReadDnaStream(FAssetArchive Ar)
    {
        var dnaStart = Ar.Position;
        var doc = DnaBinaryParser.Parse(new FArchiveBigEndian(Ar), dnaStart);
        var dnaBytes = Ar.ReadBytesAt(dnaStart, (int) doc.ByteLength);
        Ar.Position = dnaStart + doc.ByteLength;
        return (doc, dnaBytes);
    }

    protected void ParseDnaStream(FAssetArchive Ar, long limitPos, bool parseTrailingSnapshot)
    {
        var (doc, dnaBytes) = ReadDnaStream(Ar);
        ApplyDocument(doc, dnaBytes);

        if (parseTrailingSnapshot && Ar.Position < limitPos)
        {
            var snapshotBytes = Ar.ReadBytes((int) (limitPos - Ar.Position));
            try
            {
                Snapshot = RigLogicSnapshot.Read(snapshotBytes, requireFullConsume: false);
            }
            catch (Exception e)
            {
                // The DNA itself is still usable without the cooked RigLogic state
                Log.Warning(e, "Failed to read the RigLogic snapshot of {0}", Name);
            }
        }
    }

    /// <summary>
    /// Before custom versions were added, the asset stored its behavior and its geometry as two DNA files. UE keeps the
    /// behavior layers of the first one and the geometry of the second, which cooked assets only have as an empty placeholder.
    /// </summary>
    protected void ParseLegacyDnaStreams(FAssetArchive Ar, long limitPos)
    {
        var (behavior, dnaBytes) = ReadDnaStream(Ar);
        if (Ar.Position < limitPos && DnaBinaryParser.LooksLikeDna(Ar))
        {
            try
            {
                var (geometry, _) = ReadDnaStream(Ar);
                if (DnaAssetQueries.TryGet(geometry.Layers, "geom", out RawGeometry geom))
                    behavior.Layers["geom"] = geom;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to read the geometry DNA of {0}", Name);
            }
        }

        ApplyDocument(behavior, dnaBytes);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Version));
        serializer.Serialize(writer, Version);

        if (Layers is not null && DnaAssetQueries.TryGet(Layers, "desc", out RawDescriptor descriptor))
        {
            writer.WritePropertyName("Descriptor");
            serializer.Serialize(writer, descriptor);
        }
    }
}

public class UDNAAsset : UDNAObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DnaFileName = GetOrDefault(nameof(DnaFileName), GetOrDefault<string>("DnaFileName_DEPRECATED"));

        var customVer = FDNAAssetCustomVersion.Get(Ar);
        if (customVer < FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded) return;

        if (customVer == FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            ParseLegacyDnaStreams(Ar, validPos);
            return;
        }

        ParseDnaStream(Ar, validPos, parseTrailingSnapshot: false);
    }
}

public class UDNA : UDNAObject
{
    public FDNAConfig? DNAConfig;
    public bool bKeepDNAAfterInitialization;
    public bool bUseOptimizedCooking = true;
    public FRigLogicConfiguration? RigLogicConfiguration;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        DNAConfig = GetOrDefault<FDNAConfig>(nameof(DNAConfig));
        bKeepDNAAfterInitialization = GetOrDefault<bool>(nameof(bKeepDNAAfterInitialization));
        bUseOptimizedCooking = GetOrDefault(nameof(bUseOptimizedCooking), true);
        RigLogicConfiguration = GetOrDefault<FRigLogicConfiguration>(nameof(RigLogicConfiguration));
        

        if (Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
            return;

        var customVer = FDNAAssetCustomVersion.Get(Ar);
        if (customVer < FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
            return;

        if (customVer == FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            ParseLegacyDnaStreams(Ar, validPos);
            return;
        }

        var optimized = bUseOptimizedCooking
                        && Ar.IsLoadingFromCookedPackage
                        && customVer >= FDNAAssetCustomVersion.Type.IntroduceOptimizedSerializationDuringCooking;

        if (optimized)
        {
            if (!Ar.ReadBoolean())
                return;
            ParseDnaStream(Ar, validPos, parseTrailingSnapshot: true);
            return;
        }

        ParseDnaStream(Ar, validPos, parseTrailingSnapshot: false);
    }
}

public class UDNAAssetUserData : UObject
{
    public UDNA? DNAAsset;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DNAAsset = GetOrDefault<UDNA>(nameof(DNAAsset));
    }
}
