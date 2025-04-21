using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.PoseAsset.Conversion;

public record CPoseKey(string BoneName, FVector Location, FQuat Rotation, FVector Scale);