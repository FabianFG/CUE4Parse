using CUE4Parse.GameTypes.AoC.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.StructUtils;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FInstancedStructConverter))]
public class FInstancedStruct : IUStruct
{
    public FStructFallback? NonConstStruct => ScriptStruct?.StructType as FStructFallback;
    [Obsolete("Deprecated, please use ScriptStruct field", true)]
    public IUStruct? NonConstIUSturct => ScriptStruct?.StructType;
    public readonly FScriptStruct? ScriptStruct;

    public FInstancedStruct(FAssetArchive Ar)
    {
        if (FInstancedStructCustomVersion.Get(Ar) < FInstancedStructCustomVersion.Type.CustomVersionAdded)
        {
            const uint LegacyEditorHeader = 0xABABABAB;
            var headerOffset = Ar.Position;
            var header = Ar.Read<uint>();

            if (header != LegacyEditorHeader)
            {
                Ar.Position = headerOffset;
            }

            _ = Ar.Read<byte>(); // Old Version
        }

        ScriptStruct = Ar.Game switch
        {
            GAME_VEIN => new FScriptStruct(new FRawStruct<string>(Ar.ReadFString)),
            GAME_AshesOfCreation when Ar is FAoCDBCReader AoCReader => AoCReader.ReadInstancedStruct(),
            _ => FScriptStruct.ReadInstancedStruct(Ar)
        };
    }
}
