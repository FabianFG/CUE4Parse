using CUE4Parse.GameTypes.Borderlands4.Assets.Objects;
using CUE4Parse.GameTypes.Borderlands4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Borderlands4.Kismet;

public class EX_GbxDefPtr : KismetExpression<FGbxDefPtr>
{
    public override EExprToken Token => EExprToken.EX_FD;
    public EX_GbxDefPtr(FKismetArchive Ar)
    {
        var name = Ar.ReadFName();
        var value = new FPackageIndex(Ar);
        Value = new FGbxDefPtr(name, value);
    }
}

public class EX_GameDataHandle : KismetExpression<FGameDataHandle>
{
    public override EExprToken Token => EExprToken.EX_FE;
    public EX_GameDataHandle(FKismetArchive Ar)
    {
        var flags = Ar.Read<uint>();
        var name = Ar.ReadFName();
        Value = new FGameDataHandle(flags, name);
    }
}

public class EX_DamageSourceContainer : KismetExpression<FDamageSourceContainer>
{
    public override EExprToken Token => EExprToken.EX_F9;
    public FPackageIndex Type;
    public byte[] DamageSourceContainer;

    public EX_DamageSourceContainer(FKismetArchive Ar)
    {
        Type = new FPackageIndex(Ar);
        DamageSourceContainer = Ar.ReadArray<byte>();
        _ = Ar.Read<byte>(); // 0x30 end struct
    }
}
