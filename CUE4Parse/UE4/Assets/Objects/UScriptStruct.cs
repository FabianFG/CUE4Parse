using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class UScriptStruct
    {
        public readonly IUStruct StructType;

        public UScriptStruct(FAssetArchive Ar, string? structName)
        {
            StructType = structName switch
            {
                "IntPoint" => Ar.Read<FIntPoint>(),
                "Guid" => Ar.Read<FGuid>(),
                "GameplayTagContainer" => new FGameplayTagContainer(Ar),
                "Color" => Ar.Read<FColor>(),
                "LinearColor" => Ar.Read<FLinearColor>(),
                "SoftObjectPath" => new FSoftObjectPath(Ar),
                "SoftClassPath" => new FSoftObjectPath(Ar),
                "Vector" => Ar.Read<FVector>(),
                "Vector2D" => Ar.Read<FVector2D>(),
                "Vector4" => Ar.Read<FVector4>(),
                
                "IntVector" => Ar.Read<FIntVector>(),
                _  => throw new System.NotImplementedException()
            };
        }
    }
}
