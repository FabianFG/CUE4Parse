using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    public class FCurveMetaData
    {
        public readonly FAnimCurveType Type;
        public readonly FName[] LinkedBones;
        public readonly byte MaxLOD;

        public FCurveMetaData(FAssetArchive Ar, FAnimPhysObjectVersion.Type FrwAniVer)
        {
            Type = new FAnimCurveType(Ar);
            LinkedBones = Ar.ReadArray(Ar.Read<int>(), () => Ar.ReadFName());
            if (FrwAniVer >= FAnimPhysObjectVersion.Type.AddLODToCurveMetaData)
            {
                MaxLOD = Ar.Read<byte>();
            }
        }
    }
}
