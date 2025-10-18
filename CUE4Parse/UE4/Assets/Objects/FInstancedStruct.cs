using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FInstancedStructConverter))]
public class FInstancedStruct : IUStruct
{
    public FStructFallback NonConstStruct => NonConstIUSturct as FStructFallback ?? new FStructFallback();
    public readonly IUStruct? NonConstIUSturct;
    public readonly string? StringData;

    public FInstancedStruct(FAssetArchive Ar)
    {
        if (FInstancedStructCustomVersion.Get(Ar) < FInstancedStructCustomVersion.Type.CustomVersionAdded)
        {
            var headerOffset = Ar.Position;
            var header = Ar.Read<uint>();

            const uint LegacyEditorHeader = 0xABABABAB;
            if (header != LegacyEditorHeader)
            {
                Ar.Position = headerOffset;
            }

            _ = Ar.Read<byte>(); // Old Version
        }

        if (Ar.Game is EGame.GAME_VEIN)
        {
            StringData = Ar.ReadFString();
            return;
        }

        var strucindex = new FPackageIndex(Ar);
        var serialSize = Ar.Read<int>();
        var savedPos = Ar.Position;

        if (strucindex.IsNull)
        {
            Ar.Position = savedPos + serialSize;
            return;
        }

        try
        {
            var structName = strucindex.ResolvedObject is { } obj ? obj.Name.ToString() : null;
            if (strucindex.TryLoad<UStruct>(out var struc) || structName != null)
            {
                NonConstIUSturct = new FScriptStruct(Ar, structName, struc, ReadType.NORMAL).StructType;
            }
            else
            {
                Log.Warning("Failed to read FInstancedStruct of type {0}, skipping it", strucindex.ResolvedObject?.GetFullName());
            }
        }
        catch (ParserException e)
        {
            Log.Warning(e, "Failed to read FInstancedStruct of type {0}, skipping it", strucindex.ResolvedObject?.GetFullName());
        }
        finally
        {
            Ar.Position = savedPos + serialSize;
        }
    }
}
