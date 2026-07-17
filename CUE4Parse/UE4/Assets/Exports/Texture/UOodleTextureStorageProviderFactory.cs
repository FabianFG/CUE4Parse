using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class FOodleTexture2DMipMap : FTexture2DMipMap
{
    public int Version;
    public uint OodleFlags;
    public int[] Modes = [];

    private const int ModeCountCount = 10;

    public FOodleTexture2DMipMap(FAssetArchive Ar)
    {
        SizeX = Ar.Read<int>();
        SizeY = Ar.Read<int>();
        SizeZ = Ar.Read<int>();
        if (!Ar.ReadBoolean())
        {
            BulkData = new FByteBulkData(Ar);
            return;
        }
        Version = Ar.Read<int>();
        OodleFlags = Ar.Read<uint>();
        Modes = Ar.ReadArray<int>(ModeCountCount);
        BulkData = new FByteBulkData(Ar);
    }
}

public class UOodleTextureStorageProviderFactory : UTextureAllMipDataProviderFactory
{

    public EPixelFormat Format { get; protected set; } = EPixelFormat.PF_Unknown;
    public FOodleTexture2DMipMap[] Mips { get; private set; } = [];
    public FPackageIndex Texture { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game != GAME_WutheringWaves) return;

        var pixelFormatName = Ar.ReadFName();
        if (pixelFormatName.Text == "PF_BC6H_Signed") pixelFormatName = "PF_BC6H";
        while (!pixelFormatName.IsNone)
        {
            if (!Enum.TryParse(pixelFormatName.Text, ignoreCase: true, out EPixelFormat pixelFormat))
                Log.Warning("Failed to parse pixel format: {PixelFormat}", pixelFormatName.Text);

            var skipOffset = Ar.Game switch
            {
                GAME_WutheringWaves => Ar.AbsolutePosition + Ar.Read<long>(),
                >= GAME_UE5_0 => Ar.AbsolutePosition + Ar.Read<long>(),
                >= GAME_UE4_20 => Ar.Read<long>(),
                _ => Ar.Read<int>()
            };

            if (Format == EPixelFormat.PF_Unknown)
            {
                Mips = Ar.ReadArray( () => new FOodleTexture2DMipMap(Ar));
               
                if (Ar.AbsolutePosition != skipOffset)
                {
                    Log.Warning("Texture2D read incorrectly. Offset {Offset}, Skip Offset {SkipOffset}, Bytes remaining {BytesRemaining}", Ar.AbsolutePosition, skipOffset, skipOffset - Ar.AbsolutePosition);
                    Ar.SeekAbsolute(skipOffset, SeekOrigin.Begin);
                }

                Format = pixelFormat;
            }
            else
            {
#if DEBUG
                Log.Debug("Skipping data for format {Format}", pixelFormatName);
#endif
                Ar.SeekAbsolute(skipOffset, SeekOrigin.Begin);
            }

            pixelFormatName = Ar.ReadFName();
        }

        Texture = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(Texture));
        serializer.Serialize(writer, Texture);
        writer.WritePropertyName(nameof(Format));
        writer.WriteValue(Format);
        writer.WritePropertyName(nameof(Mips));
        serializer.Serialize(writer, Mips);
    }
}
