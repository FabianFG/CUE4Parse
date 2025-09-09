using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class GFPStaticMeshRenderData : FStaticMeshRenderData
{
    public GFPStaticMeshRenderData(FAssetArchive Ar, bool bIsStreamable)
    {
        if (bIsStreamable)
        {
            var size = Ar.Read<int>();
            LODs = new FStaticMeshLODResources[size];
            for (var i = 0; i < size; i++)
            {
                var bulkData = new FByteBulkData(Ar);
                if (bulkData.Header.ElementCount > 0 && bulkData.Data != null)
                {
                    var tempAr = new FByteArchive("StaticMeshLODResources", bulkData.Data, Ar.Versions);
                    LODs[i] = new FStaticMeshLODResources(tempAr);
                }
            }
        }
        else
        {
            LODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
        }

        var stripDataFlags = Ar.Read<FStripDataFlags>();
        var stripped = stripDataFlags.IsAudioVisualDataStripped();
        if (Ar.Game >= EGame.GAME_UE4_21)
        {
            stripped |= stripDataFlags.IsClassDataStripped(0x01);
        }

        if (!stripped)
        {
            for (var i = 0; i < LODs.Length; i++)
            {
                var bValid = Ar.ReadBoolean();
                if (bValid) _ = new FDistanceFieldVolumeData(Ar);
            }
        }

        Bounds = new FBoxSphereBounds(Ar);

        if (Ar.Versions["StaticMesh.HasLODsShareStaticLighting"])
        {
            bLODsShareStaticLighting = Ar.ReadBoolean();
        }

        ScreenSize = Ar.ReadArray<float>(MAX_STATIC_LODS_UE4);
    }
}
