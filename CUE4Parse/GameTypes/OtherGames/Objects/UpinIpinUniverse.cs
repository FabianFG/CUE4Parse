using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public enum ESUDSValueType : byte
{
    Text = 0,
    Int = 1,
    Float = 2,
    Boolean = 3,
    Gender = 4,
    Name = 5,
    Variable = 10,
    Empty = 99,
};

public class FSUDSValue : IUStruct
{
    public ESUDSValueType Type;
    public object? Value;

    public FSUDSValue(FAssetArchive Ar)
    {
        Type = Ar.Read<ESUDSValueType>();
        if (Type is ESUDSValueType.Name or ESUDSValueType.Text or ESUDSValueType.Variable or ESUDSValueType.Empty)
            Ar.Position += 4;

        Value = Type switch
        {
            ESUDSValueType.Text => new FText(Ar),
            ESUDSValueType.Int => Ar.Read<int>(),
            ESUDSValueType.Float => Ar.Read<float>(),
            ESUDSValueType.Boolean => Ar.ReadBoolean(),
            //ESUDSValueType.Gender => Ar.Read<FName>(),
            ESUDSValueType.Name or ESUDSValueType.Variable => Ar.ReadFString(),
            ESUDSValueType.Empty => null,
        };
    }
}
