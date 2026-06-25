using CUE4Parse.GameTypes.LotF.Assets.Objects.Mutables;
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
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FProgram
{
    /** Location in the m_byteCode of the beginning of each operation. */
    [JsonIgnore] public uint[] OpAddress;
    /** Byte-coded representation of the program, using variable-sized op data. */
    [JsonIgnore] public byte[] ByteCode;
    public FState[] States;
    /** Data for every rom. Deprecated in UE5.6. */
    public FRomData[] Roms_Deprecated = [];
    /** Data for every rom required in-game. */
    public FRomDataRuntime[] Roms = [];
    /** Data for every rom required at compile-time. It is empty in cooked data. */
    public FRomDataCompile[] RomsCompileData = [];
    /** Constant image mip data is split in 2 sets: ConstantImageLODsPermanent constains data that is always loaded.
        * Index with FConstantResourceIndex::Index, when Streamable is 0. */
    public FImage[] ConstantImageLODsPermanent;
    /** Constant image mip chain indices: ranges in this array are defined in FImageLODRange and the indices here refer to ConstantImageLODs. */
    public FConstantResourceIndex[] ConstantImageLODIndices;
    /** Constant image data. */
    public FImageLODRange[] ConstantImages;
    /** Constant mesh data is split in 2 sets: ConstantMeshesPermanent constains data that is always loaded.
        * Index with FConstantResourceIndex::Index, when Streamable is 0. */
    public FMesh[] ConstantMeshesPermanent;
    /** Constant mesh content indices: ranges in this array are defined in FMeshContentRange and the indices here refer to ConstantMeshes. */
    public FConstantResourceIndex[] ConstantMeshContentIndices;
    /** Constant mesh data */
    public FMeshContentRange[] ConstantMeshes;

    /** Constant mesh data: the first is the index in m_roms for each mesh or -1 if it is always loaded. Deprecated in UE5.6. */
    public List<KeyValuePair<int, FMesh?>> ConstantMeshes_Deprecated;
    /** Constant FExtensionData. */
    public FExtensionDataConstant[] ConstantExtensionData;
    /** Constant string data */
    public string[] ConstantStrings;
    public uint[][]? ConstantUInt32Lists;
    public int[][]? ConstantInt32Lists;
    public ulong[][]? ConstantUInt64Lists;
    public float[][]? ConstantFloatLists;
    public bool[][]? ConstantBoolLists;
    /** Constant layout data */
    public FLayout[] ConstantLayouts;
    /** Constant projectors */
    public FProjector[] ConstantProjectors;
    /** Constant matrices, usually used for transforms */
    public FMatrix[] ConstantMatrices;
    /** Constant shapes */
    public FShape[] ConstantShapes;
    /** Constant curves */
    public FRichCurve[] ConstantCurves;
    /** Constant skeletons */
    public FSkeleton[] ConstantSkeletons;
    /** Constant Physics Bodies */
    public FPhysicsBody[]? ConstantPhysicsBodies;
    /** FParameters of the model. The value stored here is the default value. */
    public FParameterDesc[] Parameters;
    /** Ranges for iteration of the model operations. */
    public FRangeDesc[] Ranges;
    /**
     * List of parameter lists. These are used in several places, like storing the
     * pregenerated list of parameters influencing a resource.
     * The parameter lists are sorted.
     */
    public ushort[][] ParameterLists;
    /** Given an instruction, parameters that are in the subtree. */
    public Dictionary<uint, int>? RelevantParameterList;
    /** Constant Material Data */
    public FMaterial[]? ConstantMaterials;
    /** Constant FNames */
    public Dictionary<uint, FName>? ConstantNames;
    /** Constant Mesh Sockets */
    public Dictionary<uint, FMeshSocket>? ConstantSockets;
    /** Game Dependent Data */
    public object? CustomData;

    public FProgram(FMutableArchive Ar)
    {
        if (Ar.Game is EGame.GAME_LordsoftheFallen) Ar.Position += 20;

        OpAddress = Ar.Game >= EGame.GAME_UE5_8 ? [] : Ar.ReadArray<uint>();
        ByteCode = Ar.ReadArray<byte>();
        States = Ar.ReadArray(() => new FState(Ar));
        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            Roms = Ar.ReadArray<FRomDataRuntime>();
            RomsCompileData = Ar.ReadArray<FRomDataCompile>();
        }
        else if (Ar.Game is EGame.GAME_LordsoftheFallen)
        {
            Roms = Ar.ReadArray<FRomDataRuntime>(); // all -1
            var customData = new FLotFCustomProgram(Ar);
            ConstantStrings = Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadArray(Ar.ReadFString) : Ar.ReadArray(Ar.ReadString);
            ConstantLayouts = Ar.ReadPtrArray(() => new FLayout(Ar));
            ConstantProjectors = Ar.ReadArray<FProjector>();
            ConstantMatrices = Ar.ReadArray(() => new FMatrix(Ar, false));
            ConstantShapes = Ar.ReadArray<FShape>();
            ConstantCurves = Ar.ReadArray(() => new FRichCurve() { Keys = Ar.ReadArray(() => new FRichCurveKey(Ar)), DefaultValue = Ar.Read<float>() });
            customData.Unknown = Ar.ReadArray(() => Ar.ReadArray(() => (Ar.Read<int>(), Ar.Read<int>())));
            CustomData = customData;
            ConstantSkeletons = Ar.ReadPtrArray(() => new FSkeleton(Ar));
            if (Ar.Game < EGame.GAME_UE5_7) ConstantPhysicsBodies = Ar.ReadPtrArray(() => new FPhysicsBody(Ar));
            Parameters = Ar.ReadArray(() => new FParameterDesc(Ar));
            Ranges = Ar.ReadArray(() => new FRangeDesc(Ar));
            ParameterLists = Ar.ReadArray(Ar.ReadArray<ushort>);
            return;
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
            ConstantMeshes_Deprecated = new List<KeyValuePair<int, FMesh?>>(num);
            for (int i = 0; i < num; i++)
            {
                // if index is -1 then mesh data is inlined, otherwise index is just index into romdata
                var index = Ar.Read<int>();
                ConstantMeshes_Deprecated.Add(new(index, Ar.ReadPtr(() => new FMesh(Ar))));
            }
        }
        if (Ar.Game is >= EGame.GAME_UE5_3 and < EGame.GAME_UE5_8) ConstantExtensionData = Ar.ReadArray(() => new FExtensionDataConstant(Ar));
        ConstantStrings = Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadArray(Ar.ReadFString) : Ar.ReadArray(Ar.ReadString);
        if (Ar.Game >= EGame.GAME_UE5_8)
        {
            ConstantUInt32Lists = Ar.ReadArray(Ar.ReadArray<uint>);
            ConstantInt32Lists = Ar.ReadArray(Ar.ReadArray<int>);
            ConstantUInt64Lists = Ar.ReadArray(Ar.ReadArray<ulong>);
            ConstantFloatLists = Ar.ReadArray(Ar.ReadArray<float>);
            ConstantBoolLists = Ar.ReadArray(() => Ar.ReadArray(Ar.ReadFlag));
        }
        else if (Ar.Game >= EGame.GAME_UE5_7)
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
            ConstantCurves = Ar.ReadArray(() => new FRichCurve() { Keys = Ar.ReadArray(() => new FRichCurveKey(Ar)), DefaultValue = Ar.Read<float>() });
        }
        ConstantSkeletons = Ar.ReadPtrArray(() => new FSkeleton(Ar));
        if (Ar.Game < EGame.GAME_UE5_7) ConstantPhysicsBodies = Ar.ReadPtrArray(() => new FPhysicsBody(Ar));
        Parameters = Ar.ReadArray(() => new FParameterDesc(Ar));
        Ranges = Ar.ReadArray(() => new FRangeDesc(Ar));
        ParameterLists = Ar.ReadArray(Ar.ReadArray<ushort>);
        if (Ar.Game >= EGame.GAME_UE5_8) RelevantParameterList = Ar.ReadMap(Ar.Read<uint>, Ar.Read<int>);
        if (Ar.Game >= EGame.GAME_UE5_7) ConstantMaterials = Ar.ReadPtrArray(() => new FMaterial(Ar));
        if (Ar.Game >= EGame.GAME_UE5_8)
        {
            ConstantNames = Ar.ReadMap(Ar.Read<uint>, Ar.ReadFName);
            ConstantSockets = Ar.ReadMap(Ar.Read<uint>, () => new FMeshSocket(Ar));
        }
    }
}
