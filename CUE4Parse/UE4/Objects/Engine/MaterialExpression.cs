using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class FMaterialInput<T> : FExpressionInput where T : struct
    {
        public bool UseConstant { get; protected set; }
        public T Constant { get; protected set; }

        public FMaterialInput()
        {
            UseConstant = false;
            Constant = new T();
        }

        public FMaterialInput(FAssetArchive Ar) : base(Ar)
        {
            UseConstant = Ar.ReadBoolean();
            Constant = Ar.Read<T>();
        }
    }

    public class FExpressionInput : IUStruct
    {
        public readonly int OutputIndex;
        public readonly FName InputName;
        public readonly int Mask;
        public readonly int MaskR;
        public readonly int MaskG;
        public readonly int MaskB;
        public readonly int MaskA;
        public readonly FName ExpressionName;

        public FExpressionInput()
        {
            
        }

        public FExpressionInput(FAssetArchive Ar)
        {
            /*if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize)
            {
                // TODO use property serialization instead
            }*/

            OutputIndex = Ar.Read<int>();
            InputName = FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize ? Ar.ReadFName() : new FName(Ar.ReadFString());
            Mask = Ar.Read<int>();
            MaskR = Ar.Read<int>();
            MaskG = Ar.Read<int>();
            MaskB = Ar.Read<int>();
            MaskA = Ar.Read<int>();
            if (Ar.Owner.HasFlags(EPackageFlags.PKG_FilterEditorOnly))
            {
                ExpressionName = Ar.ReadFName();
            }
        }
    }
}
