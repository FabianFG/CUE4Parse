using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Image;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Layout;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeleton;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FProgram
{
    public uint[] OpAddress;
    public byte[] ByteCode;
    public FState[] States;
    public FRomData[] Roms;
    public FImage[] ConstantImageLODs;
    public uint[] ConstantImageLODIndices;
    public FImageLODRange[] ConstantImages;
    public FMesh[] ConstantMeshes;
    public FExtensionDataConstant[] ConstantExtensionData;
    public string[] ConstantStrings;
    public FLayout[] ConstantLayouts;
    public FProjector[] ConstantProjectors;
    public FMatrix[] ConstantMatrices;
    public FShape[] ConstantShapes;
    public FRichCurve[] ConstantCurves;
    public FSkeleton[] ConstantSkeletons;
    public FPhysicsBody[] ConstantPhysicsBodies;
    public FParameterDesc[] Parameters;
    public FRangeDesc[] Ranges;
    public ushort[][] ParameterLists;

    public FProgram(FArchive Ar)
    {
        OpAddress = Ar.ReadArray<uint>();
        ByteCode = Ar.ReadArray<byte>();
        States = Ar.ReadArray(() => new FState(Ar));
        Roms = Ar.ReadArray(() => new FRomData(Ar));
        ConstantImageLODs = Ar.ReadMutableArray(() => new FImage(Ar));
        ConstantImageLODIndices = Ar.ReadArray<uint>();
        ConstantImages = Ar.ReadArray(() => new FImageLODRange(Ar));
        ConstantMeshes = Ar.ReadMutableArray(() => new FMesh(Ar));
        ConstantExtensionData = Ar.ReadArray(() => new FExtensionDataConstant(Ar));
        ConstantStrings = Ar.ReadArray(Ar.ReadMutableFString);
        ConstantLayouts = Ar.ReadMutableArray(() => new FLayout(Ar));
        ConstantProjectors = Ar.ReadArray(() => new FProjector(Ar));
        ConstantMatrices = Ar.ReadArray(() => new FMatrix(Ar));
        ConstantShapes = Ar.ReadArray(() => new FShape(Ar));
        ConstantCurves = Ar.ReadArray(() => new FRichCurve(Ar));
        ConstantSkeletons = Ar.ReadMutableArray(() => new FSkeleton(Ar));
        ConstantPhysicsBodies = Ar.ReadMutableArray(() => new FPhysicsBody(Ar));
        Parameters = Ar.ReadArray(() => new FParameterDesc(Ar));
        Ranges = Ar.ReadArray(() => new FRangeDesc(Ar));
        ParameterLists = Ar.ReadArray(Ar.ReadArray<ushort>);
    }
}
