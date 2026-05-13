using System.IO;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.GameTypes.DFHO.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Internationalization;

public class UStringTable : UObject
{
    public FStringTable StringTable { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        StringTable = new FStringTable(Ar);
        if (Ar.Game is EGame.GAME_DeltaForce && StringTable.KeysToEntries.Count == 0 &&
            Ar.Owner?.Provider is IVfsFileProvider provider && provider.TryCreateReader(Path.ChangeExtension(Ar.Name, "ustbin"), out var reader))
        {
            var deltaStringTable = new FDeltaStringTable(reader);
            StringTable.TableNamespace = deltaStringTable.TableNamespace;
            StringTable.KeysToEntries = deltaStringTable.KeysToEntries;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("StringTable");
        serializer.Serialize(writer, StringTable);
    }
}
