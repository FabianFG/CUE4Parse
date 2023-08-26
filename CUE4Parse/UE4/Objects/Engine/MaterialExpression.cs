using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Engine
{
    [StructFallback]
    public class FMaterialInput<T> : FExpressionInput where T : struct
    {
        public bool UseConstant { get; protected set; }
        public T Constant { get; protected set; }

        public FMaterialInput()
        {
            UseConstant = false;
            Constant = new T();
        }

        public FMaterialInput(FStructFallback fallback) : base(fallback)
        {
            UseConstant = fallback.GetOrDefault(nameof(UseConstant), false);
            Constant = fallback.GetOrDefault(nameof(Constant), new T());
        }

        public FMaterialInput(FAssetArchive Ar) : base(Ar)
        {
            if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize)
            {
                var fallback = new FMaterialInput<T>(new FStructFallback(Ar, "MaterialInput"));
                UseConstant = fallback.UseConstant;
                Constant = fallback.Constant;
                return;
            }

            UseConstant = Ar.ReadBoolean();
            Constant = Ar.Read<T>();
        }
    }

    [StructFallback]
    public class FMaterialInputVector : FExpressionInput
    {
        public bool UseConstant { get; protected set; }
        public FVector Constant { get; protected set; }

        public FMaterialInputVector()
        {
            UseConstant = false;
            Constant = FVector.ZeroVector;
        }

        public FMaterialInputVector(FStructFallback fallback)
        {
            UseConstant = fallback.GetOrDefault(nameof(UseConstant), false);
            Constant = fallback.GetOrDefault(nameof(Constant), FVector.ZeroVector);
        }

        public FMaterialInputVector(FAssetArchive Ar) : base(Ar)
        {
            if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize)
            {
                var fallback = new FMaterialInputVector(new FStructFallback(Ar, "MaterialMaterialInput"));
                UseConstant = fallback.UseConstant;
                Constant = fallback.Constant;
                return;
            }

            UseConstant = Ar.ReadBoolean();
            Constant = Ar.Read<FVector>();
        }
    }

    [StructFallback]
    public class FMaterialInputVector2D : FExpressionInput
    {
        public bool UseConstant { get; protected set; }
        public FVector2D Constant { get; protected set; }

        public FMaterialInputVector2D()
        {
            UseConstant = false;
            Constant = FVector2D.ZeroVector;
        }

        public FMaterialInputVector2D(FStructFallback fallback)
        {
            UseConstant = fallback.GetOrDefault(nameof(UseConstant), false);
            Constant = fallback.GetOrDefault(nameof(Constant), FVector2D.ZeroVector);
        }

        public FMaterialInputVector2D(FAssetArchive Ar) : base(Ar)
        {
            if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize)
            {
                var fallback = new FMaterialInputVector2D(new FStructFallback(Ar, "MaterialInputVector2D"));
                UseConstant = fallback.UseConstant;
                Constant = fallback.Constant;
                return;
            }

            UseConstant = Ar.ReadBoolean();
            Constant = Ar.Read<FVector2D>();
        }
    }

    [StructFallback]
    public class FExpressionInput : IUStruct
    {
        public readonly FPackageIndex? Expression;
        public readonly int OutputIndex;
        public readonly FName InputName;
        public readonly int Mask;
        public readonly int MaskR;
        public readonly int MaskG;
        public readonly int MaskB;
        public readonly int MaskA;
        public readonly FName ExpressionName;

        public FExpressionInput() { }

        public FExpressionInput(FStructFallback fallback)
        {
            Expression = fallback.GetOrDefault(nameof(Expression), new FPackageIndex());
            OutputIndex = fallback.GetOrDefault(nameof(OutputIndex), 0);
            InputName = fallback.GetOrDefault(nameof(InputName), default(FName));
            Mask = fallback.GetOrDefault(nameof(Mask), 0);
            MaskR = fallback.GetOrDefault(nameof(MaskR), 0);
            MaskG = fallback.GetOrDefault(nameof(MaskG), 0);
            MaskB = fallback.GetOrDefault(nameof(MaskB), 0);
            MaskA = fallback.GetOrDefault(nameof(MaskA), 0);
            ExpressionName = fallback.GetOrDefault(nameof(ExpressionName), default(FName));
        }

        public FExpressionInput(FAssetArchive Ar)
        {
            if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize)
            {
                var fallback = new FExpressionInput(new FStructFallback(Ar, "ExpressionInput"));
                Expression = fallback.Expression;
                OutputIndex = fallback.OutputIndex;
                InputName = fallback.InputName;
                Mask = fallback.Mask;
                MaskR = fallback.MaskR;
                MaskG = fallback.MaskG;
                MaskB = fallback.MaskB;
                MaskA = fallback.MaskA;
                ExpressionName = fallback.ExpressionName;
                return;
            }

            if (Ar is { Game: < EGame.GAME_UE5_1, IsFilterEditorOnly: false } || Ar.Game >= EGame.GAME_UE5_1)
                Expression = new FPackageIndex(Ar);
            OutputIndex = Ar.Read<int>();
            InputName = FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.PinsStoreFName ? Ar.ReadFName() : new FName(Ar.ReadFString());
            Mask = Ar.Read<int>();
            MaskR = Ar.Read<int>();
            MaskG = Ar.Read<int>();
            MaskB = Ar.Read<int>();
            MaskA = Ar.Read<int>();
            ExpressionName = Ar is { Game: <= EGame.GAME_UE5_1, IsFilterEditorOnly: true } ? Ar.ReadFName() : (Expression ?? new FPackageIndex()).Name.SubstringAfterLast('/');
        }
    }

    public class UMaterialExpression : Assets.Exports.UObject
    {
        public FPackageIndex? Material { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Material = GetOrDefault<FPackageIndex>(nameof(Material));
        }
    }
}
