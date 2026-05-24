using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.V2.Writers.ActorX.Structs.Animations;

public record CPoseKey(string BoneName, FVector Location, FQuat Rotation, FVector Scale);
