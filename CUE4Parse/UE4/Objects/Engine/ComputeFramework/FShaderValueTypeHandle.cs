using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine.ComputeFramework;

public struct FShaderValueTypeHandle : IUStruct
{
    public FName Name;
    public bool bIsDynamicArray = false;
    public FStructElement[] StructElements = [];
    public EShaderFundamentalType Type = EShaderFundamentalType.Bool;
    public EShaderFundamentalDimensionType DimensionType = EShaderFundamentalDimensionType.Scalar;
    public byte VectorElemCount;
    public byte MatrixRowCount;
    public byte MatrixColumnCount;

    public FShaderValueTypeHandle(FAssetArchive Ar)
    {
        Type = Ar.Read<EShaderFundamentalType>();

        //if (FComputeFrameworkObjectVersion.Get(Ar) >= FComputeFrameworkObjectVersion.Type.InitialVersion)
        if(Ar.Game >= EGame.GAME_UE5_1)
        {
            bIsDynamicArray = Ar.ReadBoolean();
        }

        if (Type == EShaderFundamentalType.Struct)
        {
            StructElements = Ar.ReadArray(() => new FStructElement(Ar));
        }
        else
        {
            DimensionType = Ar.Read<EShaderFundamentalDimensionType>();
            if (DimensionType == EShaderFundamentalDimensionType.Vector)
            {
                VectorElemCount = Ar.Read<byte>();
            }
            else if (DimensionType == EShaderFundamentalDimensionType.Matrix)
            {
                MatrixRowCount = Ar.Read<byte>();
                MatrixColumnCount = Ar.Read<byte>();
            }
        }
    }

    public struct FStructElement(FAssetArchive ar)
    {
        public FName Name = ar.ReadFName();
        public EShaderFundamentalType Type = ar.Read<EShaderFundamentalType>();
    }

    public enum EShaderFundamentalType : byte
    {
        Bool = 0,
        Int = 1,
        Uint = 2,
        Float = 3,
        Struct = 4,
        None = 255,
    }

    public enum EShaderFundamentalDimensionType : byte
    {
        Scalar,
        Vector,
        Matrix,
    }
}
