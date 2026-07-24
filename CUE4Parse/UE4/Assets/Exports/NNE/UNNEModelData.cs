using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.NNE;

public class UNNEModelData : UObject
{
    public string[] TargetRuntimes;
    // A string identifying the type of data inside this asset. Corresponds to the extension of the imported file.
    public string FileType;
    // The raw binary file data of the imported model.
    public byte[] FileData;
    // Additional raw binary data of the imported model.
    public Dictionary<string, byte[]>? AdditionalFileData;
    // Mapping between a runtime name and the serialized version of it's runtime settings.
    public Dictionary<string, byte[]>? RuntimeSettings;
    // A Guid that uniquely identifies this model. This is used to cache optimized models in the editor.
    public FGuid FileId;
    // The processed / optimized model data for the different runtimes.
    public Dictionary<string, byte[]> ModelData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        var version = NNEModelDataVersion.Get(Ar);
        switch (version)
        {
            case NNEModelDataVersion.Type.V0:
            case NNEModelDataVersion.Type.V1:
                FileType = Ar.ReadFString();
                FileData = Ar.ReadArray<byte>();
                FileId = Ar.Read<FGuid>();
                ModelData = Ar.ReadMap(Ar.ReadFString, Ar.ReadArray<byte>);
                break;
            case NNEModelDataVersion.Type.V2:
                TargetRuntimes = Ar.ReadArray(Ar.ReadFString);
                FileType = Ar.ReadFString();
                FileData = Ar.ReadArray<byte>();
                FileId = Ar.Read<FGuid>();
                var numItems = Ar.Read<int>();
                ModelData = [];
                for (var i = 0; i < numItems; i++)
                {
                    var name = Ar.ReadFString();
                    var memoryAlignment = Ar.Read<int>();
                    var dataSize = Ar.Read<int>();
                    ModelData[name] = Ar.ReadArray<byte>(dataSize);
                }
                break;
            case NNEModelDataVersion.Type.V3:
                TargetRuntimes = Ar.ReadArray(Ar.ReadFString);
                FileType = Ar.ReadFString();
                FileData = Ar.ReadArray<byte>();
                AdditionalFileData = Ar.ReadMap(Ar.ReadFString, Ar.ReadArray<byte>);
                FileId = Ar.Read<FGuid>();
                numItems = Ar.Read<int>();
                ModelData = [];
                for (var i = 0; i < numItems; i++)
                {
                    var name = Ar.ReadFString();
                    var memoryAlignment = Ar.Read<uint>();
                    var dataSize = Ar.Read<ulong>();
                    ModelData[name] = Ar.ReadArray<byte>((int)dataSize);
                }
                break;
            case NNEModelDataVersion.Type.V4:
            case NNEModelDataVersion.Type.V5:
                TargetRuntimes = Ar.ReadArray(Ar.ReadFString);
                FileType = Ar.ReadFString();
                FileData = Ar.ReadArray<byte>((int)Ar.Read<ulong>());
                AdditionalFileData = Ar.ReadMap(Ar.ReadFString, () => Ar.ReadArray<byte>((int)Ar.Read<ulong>()));
                if (version >= NNEModelDataVersion.Type.V5)
                {
                    RuntimeSettings = Ar.ReadMap(Ar.ReadFString, () => Ar.ReadArray<byte>((int) Ar.Read<ulong>()));
                }   
                FileId = Ar.Read<FGuid>();
                numItems = Ar.Read<int>();
                ModelData = [];
                for (var i = 0; i < numItems; i++)
                {
                    var name = Ar.ReadFString();
                    var memoryAlignment = Ar.Read<uint>();
                    var dataSize = Ar.Read<ulong>();
                    ModelData[name] = Ar.ReadArray<byte>((int)dataSize);
                }
                break;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(TargetRuntimes));
        serializer.Serialize(writer, TargetRuntimes);
        writer.WritePropertyName(nameof(FileType));
        serializer.Serialize(writer, FileType);
        writer.WritePropertyName(nameof(FileData));
        serializer.Serialize(writer, FileData);
        writer.WritePropertyName(nameof(AdditionalFileData));
        serializer.Serialize(writer, AdditionalFileData?.Keys);
        writer.WritePropertyName(nameof(RuntimeSettings));
        serializer.Serialize(writer, RuntimeSettings?.Keys);
        writer.WritePropertyName(nameof(FileId));
        serializer.Serialize(writer, FileId);
        writer.WritePropertyName(nameof(ModelData));
        serializer.Serialize(writer, ModelData?.Keys);
    }


}

public static class NNEModelDataVersion
{
    public enum Type
    {
        V0 = 0, // Initial
        V1 = 1, // TargetRuntimes and AssetImportData
        V2 = 2, // Re-arrange fields and store only ModelData in cooked assets
        V3 = 3, // Adding AdditionalFileData
        V4 = 4, // Support for > 2GB models
        V5 = 5, // Adding RuntimeSettings

        // -----<new versions can be added above this line>-----
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0x9513202E, 0xEBA1B279, 0xF17FE5BA, 0xAB90C3F2);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < GAME_UE5_2 => (Type) (-1),
            < GAME_UE5_3 => Type.V0,
            < GAME_UE5_4 => Type.V1,
            < GAME_UE5_5 => Type.V3,
            < GAME_UE5_8 => Type.V4,
            _ => Type.LatestVersion
        };
    }
}
