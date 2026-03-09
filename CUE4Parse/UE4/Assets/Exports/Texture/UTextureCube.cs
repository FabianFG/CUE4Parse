using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UTextureCube : UTexture
{
    public FPackageIndex? FacePosX { get; private set; }
    public FPackageIndex? FaceNegX { get; private set; }
    public FPackageIndex? FacePosY { get; private set; }
    public FPackageIndex? FaceNegY { get; private set; }
    public FPackageIndex? FacePosZ { get; private set; }
    public FPackageIndex? FaceNegZ { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        FacePosX = GetOrDefault<FPackageIndex>(nameof(FacePosX));
        FaceNegX = GetOrDefault<FPackageIndex>(nameof(FaceNegX));
        FacePosY = GetOrDefault<FPackageIndex>(nameof(FacePosY));
        FaceNegY = GetOrDefault<FPackageIndex>(nameof(FaceNegY));
        FacePosZ = GetOrDefault<FPackageIndex>(nameof(FacePosZ));
        FaceNegZ = GetOrDefault<FPackageIndex>(nameof(FaceNegZ));

        if (Ar.Ver < EUnrealEngineObjectUE3Version.RENDERING_REFACTOR)
        {
            var SizeX = Ar.Read<int>();
            var SizeY = Ar.Read<int>();
            Format = (EPixelFormat) Ar.Read<int>();
            var numMips = Ar.Read<int>();
        }

        if (Ar.Game < EGame.GAME_UE4_0) return; // Nothing left
        var stripFlags = new FStripDataFlags(Ar);
        var bCooked = Ar.ReadBoolean();

        if (bCooked)
        {
            DeserializeCookedPlatformData(Ar);
        }
    }
}

public class UTextureCubeArray : UTexture
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var stripFlags = new FStripDataFlags(Ar);
        var bCooked = Ar.ReadBoolean();

        if (bCooked)
        {
            DeserializeCookedPlatformData(Ar);
        }
    }
}
