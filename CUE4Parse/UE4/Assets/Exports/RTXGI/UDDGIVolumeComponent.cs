using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.RTXGI;

// Mortal Kombat 1, Arc Raiders
public class UDDGIVolumeComponent : USceneComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FDDGICustomVersion.Get(Ar) < FDDGICustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            Log.Warning("Unknown custom version of {0}, please specify correct version in the settings", nameof(FDDGICustomVersion));
            return;
        }

        if (FDDGICustomVersion.Get(Ar) < FDDGICustomVersion.Type.AddingCustomVersion)
        {

        }
        else if (FDDGICustomVersion.Get(Ar) >= FDDGICustomVersion.Type.SaveLoadProbeTextures)
        {
            bool bSeralizeProbesIsOptional = FDDGICustomVersion.Get(Ar) >= FDDGICustomVersion.Type.SaveLoadProbeDataIsOptional;
            bool bProbesSerialized = true;
            if (bSeralizeProbesIsOptional) bProbesSerialized = Ar.ReadBoolean();
            if (bProbesSerialized)
            {
                var Irradiance = new FDDGITexturePixels(Ar);
                var Distance = new FDDGITexturePixels(Ar);
                var Offsets = new FDDGITexturePixels(Ar);
                var States = new FDDGITexturePixels(Ar);
            }
        }
    }

    public class FDDGITexturePixels
    {
        public uint Width;
        public uint Height;
        public uint Stride;
        public EPixelFormat PixelFormat;
        public byte[] Pixels;

        public FDDGITexturePixels(FArchive Ar)
        {
            Width = Ar.Read<uint>();
            Height = Ar.Read<uint>();
            Stride = Ar.Read<uint>();
            Pixels = Ar.ReadArray<byte>();
            PixelFormat = (EPixelFormat)Ar.Read<uint>();
        }
    }
}

public class FDDGICustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,

        AddingCustomVersion = 1,
        // save pixels and width/height
        SaveLoadProbeTextures,
        // save texel format since the format can change in the project settings
        SaveLoadProbeTexturesFmt,
        // Probe data is optionally stored depending on project settings
        SaveLoadProbeDataIsOptional,
        V5,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0xc12f0537, 0x7346d9c5, 0x336fbba3, 0x738ab145);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            GAME_MortalKombat1 => Type.SaveLoadProbeDataIsOptional,
            GAME_ArcRaiders => Type.V5,
            _ => (Type) (-1),
        };
    }
}
