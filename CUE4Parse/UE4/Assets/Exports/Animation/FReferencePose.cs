using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [JsonConverter(typeof(FReferencePoseConverter))]
    public struct FReferencePose
    {
        public readonly FName PoseName;
        public readonly FTransform[] ReferencePose;

        public FReferencePose(FAssetArchive Ar)
        {
            PoseName = Ar.ReadFName();
            ReferencePose = Ar.ReadArray(() => new FTransform(Ar));
        }
    }
}
