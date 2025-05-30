using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Assets.Objects;
using System.Reflection.Metadata.Ecma335;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public struct NiagaraUserParameterModifier : IUStruct
{
    public FName Name;
    public bool bUseDefaultValue;
    public IUStruct Value;

    public NiagaraUserParameterModifier(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        bUseDefaultValue = Ar.ReadBoolean();
        var type = Ar.Read<byte>();
        Value = type switch
        {
            0 => new TIntVector1<int>(Ar.Read<int>()),
            1 => new TIntVector1<float>(Ar.Read<float>()),
            2 => new TIntVector1<bool>(Ar.ReadBoolean()),
            3 => Ar.Read<FVector>(),
            4 => Ar.Read<FLinearColor>(),
            _ => throw new ParserException($"Unknown type {type} for NiagaraUserParameterModifier")
        };
    }
}
