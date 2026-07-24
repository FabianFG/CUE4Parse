using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.DBD.Objects;

public class FBhvrBarkNodeTemplate : IUStruct
{
    public FScriptStruct? Node;

    public FBhvrBarkNodeTemplate() { }
    public FBhvrBarkNodeTemplate(FAssetArchive Ar)
    {
        if (!Ar.ReadBoolean()) return;
        Node = FScriptStruct.ReadInstancedStructWithoutSerialSize(Ar);
    }
}
