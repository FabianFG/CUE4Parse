using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Materials;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Layout;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Skeleton;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Roms;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FProgram
{
    [JsonIgnore] public uint[] OpAddress;
    [JsonIgnore] public byte[] ByteCode;
    public FState[] States;
    public FRomData[] Roms_Deprecated = [];
    public FRomDataRuntime[] Roms = [];
    public FRomDataCompile[] RomsCompileData = [];
    public FImage[] ConstantImageLODsPermanent;
    public FConstantResourceIndex[] ConstantImageLODIndices;
    public FImageLODRange[] ConstantImages;
    public FMesh[] ConstantMeshesPermanent;
    public FConstantResourceIndex[] ConstantMeshContentIndices;
    public FMeshContentRange[] ConstantMeshes;
    public List<KeyValuePair<int, FMesh>> ConstantMeshes_Deprecated;
    public FExtensionDataConstant[] ConstantExtensionData;
    public string[] ConstantStrings;
    public uint[][]? ConstantUInt32Lists;
    public ulong[][]? ConstantUInt64Lists;
    public FLayout[] ConstantLayouts;
    public FProjector[] ConstantProjectors;
    public FMatrix[] ConstantMatrices;
    public FShape[] ConstantShapes;
    public FRichCurve[] ConstantCurves;
    public float[] ConstantCurvesDefaultValues = [];
    public FSkeleton[] ConstantSkeletons;
    public FPhysicsBody[]? ConstantPhysicsBodies;
    public FParameterDesc[] Parameters;
    public FRangeDesc[] Ranges;
    public ushort[][] ParameterLists;
    public Dictionary<uint, int>? RelevantParameterList;
    public FMaterial[]? ConstantMaterials;
   
    public FProgram(FMutableArchive Ar)
    {
        OpAddress = Ar.ReadArray<uint>();
        ByteCode = Ar.ReadArray<byte>();
        States = Ar.ReadArray(() => new FState(Ar));
        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            Roms = Ar.ReadArray<FRomDataRuntime>();
            RomsCompileData = Ar.ReadArray<FRomDataCompile>();
        }
        else
        {
            Roms_Deprecated = Ar.ReadArray(() => new FRomData(Ar));
        }
        ConstantImageLODsPermanent = Ar.ReadPtrArray(() => new FImage(Ar));
        ConstantImageLODIndices = Ar.ReadArray<FConstantResourceIndex>();
        ConstantImages = Ar.ReadArray<FImageLODRange>();
        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            ConstantMeshesPermanent = Ar.ReadPtrArray(() => new FMesh(Ar));
            ConstantMeshContentIndices = Ar.ReadArray<FConstantResourceIndex>();
            ConstantMeshes = Ar.ReadArray<FMeshContentRange>();
        }
        else
        {
            var num = Ar.Read<int>();
            ConstantMeshes_Deprecated = new List<KeyValuePair<int, FMesh>>(num);
            for (int i = 0; i < num; i++)
            {
                ConstantMeshes_Deprecated.Add(new(Ar.Read<int>(), Ar.ReadPtr(() => new FMesh(Ar))));
            }
        }
        if (Ar.Game >= EGame.GAME_UE5_3) ConstantExtensionData = Ar.ReadArray(() => new FExtensionDataConstant(Ar));
        // string below UE5.4, and FString after
        ConstantStrings = Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadArray(Ar.ReadFString) : Ar.ReadArray(Ar.ReadString);
        if (Ar.Game >= EGame.GAME_UE5_7)
        {
            ConstantUInt32Lists = Ar.ReadArray(Ar.ReadArray<uint>);
            ConstantUInt64Lists = Ar.ReadArray(Ar.ReadArray<ulong>);
        }
        ConstantLayouts = Ar.ReadPtrArray(() => new FLayout(Ar));
        ConstantProjectors = Ar.ReadArray<FProjector>();
        ConstantMatrices = Ar.ReadArray(() => new FMatrix(Ar, false));
        ConstantShapes = Ar.ReadArray<FShape>();
        if (Ar.Game >= EGame.GAME_UE5_5)
        {
            ConstantCurves = Ar.ReadArray(() => new FRichCurve(Ar));
        }
        else
        {
            var num = Ar.Read<int>();
            ConstantCurves = new FRichCurve[num];
            var ConstantCurvesDefaultValues = new float[num];
            for (int i = 0; i < num; i++)
            {
                var keys = Ar.ReadArray(() => new FRichCurveKey(Ar));
                ConstantCurves[i] = new FRichCurve() { Keys = keys };
                ConstantCurvesDefaultValues[i] = Ar.Read<float>();
            }
        }
        ConstantSkeletons = Ar.ReadPtrArray(() => new FSkeleton(Ar));
        if (Ar.Game < EGame.GAME_UE5_7) ConstantPhysicsBodies = Ar.ReadPtrArray(() => new FPhysicsBody(Ar));
        Parameters = Ar.ReadArray(() => new FParameterDesc(Ar));
        Ranges = Ar.ReadArray(() => new FRangeDesc(Ar));
        ParameterLists = Ar.ReadArray(Ar.ReadArray<ushort>);
        if (Ar.Game >= EGame.GAME_UE5_8) RelevantParameterList = Ar.ReadMap(Ar.Read<uint>, Ar.Read<int>);
        if (Ar.Game >= EGame.GAME_UE5_7) ConstantMaterials = Ar.ReadPtrArray(() => new FMaterial(Ar));
    }
}
