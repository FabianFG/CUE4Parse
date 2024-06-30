using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    [JsonConverter(typeof(FSkeletalMaterialConverter))]
    public class FSkeletalMaterial
    {
        public ResolvedObject? Material; // UMaterialInterface
        public FName MaterialSlotName;
        public FName? ImportedMaterialSlotName;
        public FMeshUVChannelInfo? UVChannelData;

        public FSkeletalMaterial(FAssetArchive Ar)
        {
            Material = new FPackageIndex(Ar).ResolvedObject;
            if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.RefactorMeshEditorMaterials)
            {
                MaterialSlotName = Ar.ReadFName();
                var bSerializeImportedMaterialSlotName = !Ar.Owner.HasFlags(EPackageFlags.PKG_FilterEditorOnly);
                if (FCoreObjectVersion.Get(Ar) >= FCoreObjectVersion.Type.SkeletalMaterialEditorDataStripping)
                {
                    bSerializeImportedMaterialSlotName = Ar.ReadBoolean();
                }

                if (bSerializeImportedMaterialSlotName)
                {
                    ImportedMaterialSlotName = Ar.ReadFName();
                }
            }
            else
            {
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.MOVE_SKELETALMESH_SHADOWCASTING)
                    Ar.Position += 4;

                if (FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RuntimeRecomputeTangent)
                {
                    var bRecomputeTangent = Ar.ReadBoolean();
                }
            }
            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
                UVChannelData = new FMeshUVChannelInfo(Ar);

            if (Ar.Game is EGame.GAME_CalabiYau or EGame.GAME_FragPunk) Ar.Position += 4;
        }
    }
}
