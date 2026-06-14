using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.RigVM;

public class URigVMLink : Assets.Exports.UObject
{
    public string SourcePinPath;
    public string TargetPinPath;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        SourcePinPath = Ar.ReadFString();
        TargetPinPath = Ar.ReadFString();
    }
}
