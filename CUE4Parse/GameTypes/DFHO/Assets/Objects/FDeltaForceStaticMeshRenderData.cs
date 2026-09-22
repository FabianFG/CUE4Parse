using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DFHO.Assets.Objects;

public static class FDeltaForceStaticMeshRenderData
{
    public static void SkipDistanceFields(FAssetArchive Ar, int lodCount)
    {
        var hasInlineData = false;
        var firstDistanceField = true;
        for (var i = 0; i < 6; i++)
        {
            var bValid = i == 5 && lodCount < 6 ? Ar.Read<byte>() != 0 : Ar.ReadFlag();
            if (i == 5 && !bValid)
                hasInlineData = true;
            if (!bValid)
                continue;

            var stripFlags = new FStripDataFlags(Ar);
            if (stripFlags.IsEditorDataStripped() && stripFlags.ClassStripFlags > 0)
            {
                if (i == 5)
                {
                    hasInlineData = true;
                }

                Ar.Position += (firstDistanceField ? 179 : 175) + 36 * (stripFlags.ClassStripFlags - 1);
            }
            else
            {
                var arraySize = stripFlags.GlobalStripFlags | stripFlags.ClassStripFlags << 8 | Ar.Read<ushort>() << 16;
                Ar.Position += arraySize;
                _ = Ar.Read<FIntVector>();
                _ = new FBox(Ar);
                _ = new FVector2D(Ar);
                _ = Ar.ReadBoolean();
                _ = Ar.ReadBoolean();
                _ = Ar.ReadBoolean();
            }

            firstDistanceField = false;
        }

        if (!hasInlineData)
            return;

        _ = new FStripDataFlags(Ar);
        var rayTracingSections = Ar.ReadArray(() => new FStaticMeshSection(Ar));
        if (rayTracingSections.Length > 0)
        {
            _ = Ar.ReadBoolean();
            _ = Ar.ReadBoolean();
            var shortMetadata = Ar.ReadBoolean();
            _ = new FByteBulkData(Ar);
            if (shortMetadata)
            {
                Ar.Position += 109;
            }
            else
            {
                Ar.Position += 104;
                _ = new FStripDataFlags(Ar);
                var metadataSections = Ar.ReadArray(() => new FStaticMeshSection(Ar));
                Ar.Position += 128 + 12 * metadataSections.Length;
            }
        }

        Ar.Position += 24 + lodCount;

        _ = Ar.ReadFlag();
        _ = Ar.ReadBoolean();
        _ = new FStripDataFlags(Ar);
        _ = new FByteBulkData(Ar);
        var byteCount = Ar.Read<int>();
        Ar.Position += byteCount;

        var descriptorCount = Ar.Read<int>();
        Ar.Position += 20 * descriptorCount;
        var mipCount = Ar.Read<int>();
        Ar.Position += 224 * mipCount;
        _ = Ar.ReadBoolean();
        _ = Ar.ReadBoolean();
        Ar.SkipArray<ushort>();
        Ar.Position += 16;
    }

    public static FBoxSphereBounds DeserializeCustomData(FAssetArchive Ar, int lodCount)
    {
        Ar.Position += 68;
        Ar.SkipArray<int>();
        _ = new FStripDataFlags(Ar);
        _ = Ar.ReadBoolean();

        var bounds = new FBoxSphereBounds(new FBox(Ar));

        Ar.Position += 4;
        var customDataCount = Ar.Read<int>();
        Ar.Position += 26 + 8 * lodCount + 61 * customDataCount;

        return bounds;
    }
}
