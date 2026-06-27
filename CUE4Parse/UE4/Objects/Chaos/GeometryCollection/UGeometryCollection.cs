using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class UGeometryCollection : UObject
    {
        public FGeometryCollection? GeometryCollection { get; private set; }
        public FGeometryCollectionNaniteData? RenderData { get; private set; }
        public FNaniteResources? OldNaniteData { get; private set; }
        public FPackageIndex[] Materials { get; private set; }
        public FGeometryCollectionProxyMeshData? RootProxyData { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Materials = GetOrDefault<FPackageIndex[]>(nameof(Materials), []);
            RootProxyData = GetOrDefault<FGeometryCollectionProxyMeshData>(nameof(RootProxyData));

            var bIsCookedOrCooking = FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.GeometryCollectionInDDC && Ar.ReadBoolean();
            
            if (FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.GeometryCollectionInDDCAndAsset)
            {
                GeometryCollection = new FGeometryCollection(new FChaosArchive(Ar));
            }
            //
            if (FUE5MainStreamObjectVersion.Get(Ar) == FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteData ||
                (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteCooked &&
                 FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteTransient))
            {
                // This legacy version serialized structure information into archive, but the data is transient.
                // Just load it and throw away here, it will be rebuilt later and resaved past this point.
                // OldNaniteData = 
                SerializeOldNaniteData(Ar);
            }

            // marvel rival's doing some shit here
            if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteTransient)
            {
                // if (Ar.Game == EGame.GAME_MarvelRivals)
                // {
                //     var gi = (GeometryCollection?.GroupInfo).FirstOrDefault(x => x.Key.PlainText == "Transform");
                //     if (!gi.Key.IsNone)
                //     {
                //         var num = gi.Value.Size; // num * 24?
                //         Ar.Position += (8 * 3) * num;
                //     }
                //     // more data
                // }

                var bCooked = Ar.ReadBoolean();
                if (bCooked)
                {
                    if (GetOrDefault<bool>("bStripRenderDataOnCook"))
                    {
                        Ar.Position += 4 * 2; // bool x2
                    }
                    else
                    {
                        RenderData = new FGeometryCollectionNaniteData(Ar);
                    }
                }
            }
        }

        // Parse old Nanite data and throw it away. We need this to not crash when parsing old files.
        public static void SerializeOldNaniteData(FAssetArchive Ar)
        {
            var NumNaniteResources = Ar.Read<int>();            

            for (int i = 0; i < NumNaniteResources; ++i)
            {
                var stripFlags = new FStripDataFlags(Ar);
                if (!stripFlags.IsAudioVisualDataStripped())
                {
                    bool bLZCompressed;
                    
                    bLZCompressed = Ar.ReadBoolean();
                    var RootClusterPage = Ar.ReadArray<byte>();
                    var StreamableClusterPages = new FByteBulkData(Ar);
                    var PageStreamingStates = new FPageStreamingState(Ar);
                    var HierarchyNodes = new FPackedHierarchyNode(Ar);
                    var PageDependencies = Ar.ReadArray<uint>();
                    var ImposterAtlas = Ar.ReadArray<ushort>();
                }
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);
            writer.WritePropertyName(nameof(GeometryCollection));
            
            serializer.Serialize(writer, GeometryCollection);
        }
    }
}
