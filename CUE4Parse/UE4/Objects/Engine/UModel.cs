using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Model;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Versions.EUnrealEngineObjectUE4Version;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UModel : Assets.Exports.UObject
    {
        private FBoxSphereBounds Bounds;
        private FVector[] Vectors;
        private FVector[] Points;
        private FBspNode[] Nodes;
        private FBspSurf[] Surfs;
        private FVert[] Verts;
        private int NumSharedSides;
        private bool RootOutside;
        private bool Linked;
        private uint NumUniqueVertices;
        private FModelVertexBuffer VertexBuffer;
        private FGuid LightingGuid;
        

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            const int stripVertexBufferFlag = 1;
            var stripData = new FStripDataFlags(Ar, 0); // GetOuter() && GetOuter()->IsA(ABrush::StaticClass()) ? StripVertexBufferFlag : FStripDataFlags::None

            Bounds = Ar.Read<FBoxSphereBounds>();

            Vectors = Ar.ReadBulkArray<FVector>();
            Points = Ar.ReadBulkArray<FVector>();
            Nodes = Ar.ReadBulkArray<FBspNode>();

            Surfs = Ar.ReadBulkArray<FBspSurf>();
            Verts = Ar.ReadBulkArray<FVert>();
            
            NumSharedSides = Ar.Read<int>();

            RootOutside = Ar.ReadBoolean();
            Linked = Ar.ReadBoolean();  // crashes here for some reason
            //Ar.Position += 4;

            if (Ar.Ver < (UE4Version) VER_UE4_REMOVE_ZONES_FROM_MODEL)
            {
                Ar.SkipBulkArrayData(); // TArray<int32> DummyPortalNodes
            }

            NumUniqueVertices = Ar.Read<uint>();

            if(!stripData.IsEditorDataStripped() || !stripData.IsClassDataStripped( stripVertexBufferFlag ))
            {
                VertexBuffer = new FModelVertexBuffer(Ar);
            }

            LightingGuid = Ar.Read<FGuid>();

            //Ar << LightmassSettings;
            
            Ar.Position = validPos; // TODO read it's contents, this is just to suppress warnings
        }
    }
}