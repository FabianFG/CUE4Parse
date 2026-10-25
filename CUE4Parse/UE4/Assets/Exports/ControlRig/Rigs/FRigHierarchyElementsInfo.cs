using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ControlRig.Rigs;

[StructFallback]
public readonly struct FRigHierarchyElementsInfo : IUStruct
{
    public readonly uint[] NumElementsByType;
    public readonly uint NumComponents = 0;

    public FRigHierarchyElementsInfo(FArchive Ar)
    {
        NumElementsByType = new uint[7];
        NumElementsByType[RigElementTypeToIndex(ERigElementType.Bone)] = Ar.Read<uint>();
        NumElementsByType[RigElementTypeToIndex(ERigElementType.Reference)] = Ar.Read<uint>();
        NumElementsByType[RigElementTypeToIndex(ERigElementType.Socket)] = Ar.Read<uint>();
        NumElementsByType[RigElementTypeToIndex(ERigElementType.Null)] = Ar.Read<uint>();
        NumElementsByType[RigElementTypeToIndex(ERigElementType.Control)] = Ar.Read<uint>();
        NumElementsByType[RigElementTypeToIndex(ERigElementType.Curve)] = Ar.Read<uint>();
        NumElementsByType[RigElementTypeToIndex(ERigElementType.Connector)] = Ar.Read<uint>();
        NumComponents = Ar.Read<uint>();
    }

    private static int RigElementTypeToIndex(ERigElementType type)
    {
        return type switch
        {
            ERigElementType.Bone => 0,
            ERigElementType.Null => 1,
            ERigElementType.Control => 2,
            ERigElementType.Reference => 3,
            ERigElementType.Socket => 4,
            ERigElementType.Curve => 5,
            ERigElementType.Connector => 6,
            _ => -1
        };
    }
}
