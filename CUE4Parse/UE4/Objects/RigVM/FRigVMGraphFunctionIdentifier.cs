using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMGraphFunctionIdentifier
{
    public string LibraryNodePath;
    public FSoftObjectPath HostObject;
    public FGuid? Guid;

    public FRigVMGraphFunctionIdentifier(FAssetArchive Ar)
    {
        if (FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.RemoveLibraryNodeReferenceFromFunctionIdentifier)
        {
            LibraryNodePath = new FSoftObjectPath(Ar).ToString();
        }
        else
        {
            LibraryNodePath = Ar.ReadFString();
        }
        HostObject = new FSoftObjectPath(Ar);
        // Version-gated identity Guid (on-disk format only grows). Pre-GuidForFunctions loads leave it invalid,
	    // so GetGuid() uses the deterministic-from-paths fallback until PatchFunctionGuidsOnLoad stamps one.
	    if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.GuidForFunctions)
	    {
		    Guid = Ar.Read<Guid>();
	    }
    }
}
