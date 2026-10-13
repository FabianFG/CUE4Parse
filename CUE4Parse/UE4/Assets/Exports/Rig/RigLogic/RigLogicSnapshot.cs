namespace CUE4Parse.UE4.Assets.Exports.Rig.RigLogic;

public enum RlCalculationType : byte
{
    Scalar,
    SSE,
    AVX,
    NEON,
    AnyVector
}

public enum RlTranslationType : byte
{
    Vector = 3
}

public enum RlRotationType : byte
{
    EulerAngles = 3,
    Quaternions = 4
}

public enum RlScaleType : byte
{
    Vector = 3
}

public enum RlEvaluatorType : ushort
{
    Auto,
    Null,
    Concrete
}

public sealed class RlConfiguration
{
    public RlCalculationType CalculationType;
    public bool LoadJoints;
    public bool LoadBlendShapes;
    public bool LoadAnimatedMaps;
    public bool LoadMachineLearnedBehavior;
    public bool LoadRBFBehavior;
    public bool LoadTwistSwingBehavior;
    public RlTranslationType TranslationType;
    public RlRotationType RotationType;
    public RlScaleType ScaleType;
    public float TranslationPruningThreshold;
    public float RotationPruningThreshold;
    public float ScalePruningThreshold;

    public static RlConfiguration Read(TerseBinaryArchive ar)
    {
        return new RlConfiguration
        {
            CalculationType = (RlCalculationType) ar.ReadUInt8(),
            LoadJoints = ar.ReadBool8(),
            LoadBlendShapes = ar.ReadBool8(),
            LoadAnimatedMaps = ar.ReadBool8(),
            LoadMachineLearnedBehavior = ar.ReadBool8(),
            LoadRBFBehavior = ar.ReadBool8(),
            LoadTwistSwingBehavior = ar.ReadBool8(),
            TranslationType = (RlTranslationType) ar.ReadUInt8(),
            RotationType = (RlRotationType) ar.ReadUInt8(),
            ScaleType = (RlScaleType) ar.ReadUInt8(),
            TranslationPruningThreshold = ar.ReadFloat(),
            RotationPruningThreshold = ar.ReadFloat(),
            ScalePruningThreshold = ar.ReadFloat()
        };
    }

    public int AttributesPerJoint => (byte) TranslationType + (byte) RotationType + (byte) ScaleType;
}

public sealed class RlMetadata
{
    public uint CoordX, CoordY, CoordZ;
    public uint RotationSequence;
    public int RotationSignX, RotationSignY, RotationSignZ;
    public ushort LodCount;
    public ushort GuiControlCount;
    public ushort RawControlCount;
    public ushort PsdControlCount;
    public ushort MlControlCount;
    public ushort RbfControlCount;
    public ushort JointGroupCount;
    public ushort JointAttributeCount;
    public ushort BlendShapeCount;
    public ushort AnimatedMapCount;
    public ushort MlTypeCount;
    public ushort RbfSolverCount;
    public ushort TwistCount;
    public ushort SwingCount;
    public RlEvaluatorType[] Evaluators = [];

    public static RlMetadata Read(TerseBinaryArchive ar)
    {
        var meta = new RlMetadata
        {
            CoordX = ar.ReadUInt32(),
            CoordY = ar.ReadUInt32(),
            CoordZ = ar.ReadUInt32(),
            RotationSequence = ar.ReadUInt32(),
            RotationSignX = DecodeSign(ar.ReadUInt32()),
            RotationSignY = DecodeSign(ar.ReadUInt32()),
            RotationSignZ = DecodeSign(ar.ReadUInt32()),
            LodCount = ar.ReadUInt16(),
            GuiControlCount = ar.ReadUInt16(),
            RawControlCount = ar.ReadUInt16(),
            PsdControlCount = ar.ReadUInt16(),
            MlControlCount = ar.ReadUInt16(),
            RbfControlCount = ar.ReadUInt16(),
            JointGroupCount = ar.ReadUInt16(),
            JointAttributeCount = ar.ReadUInt16(),
            BlendShapeCount = ar.ReadUInt16(),
            AnimatedMapCount = ar.ReadUInt16(),
            MlTypeCount = ar.ReadUInt16(),
            RbfSolverCount = ar.ReadUInt16(),
            TwistCount = ar.ReadUInt16(),
            SwingCount = ar.ReadUInt16(),
            Evaluators = ar.ReadArray(() => (RlEvaluatorType) ar.ReadUInt16())
        };
        return meta;
    }

    private static int DecodeSign(uint raw) => raw is 0xFFFFFFFF or 255 ? -1 : (int) raw;
}

public sealed class RlConditionalTable
{
    public RlRangeMap[] RangeMaps = [];
    public ushort[] IntervalsRemaining = [];
    public ushort[] InputIndices = [];
    public ushort[] OutputIndices = [];
    public float[] FromValues = [];
    public float[] ToValues = [];
    public float[] SlopeValues = [];
    public float[] CutValues = [];
    public ushort InputCount;
    public ushort OutputCount;

    public static RlConditionalTable Read(TerseBinaryArchive ar)
    {
        return new RlConditionalTable
        {
            RangeMaps = ar.ReadArray(() => RlRangeMap.Read(ar)),
            IntervalsRemaining = ar.ReadUInt16Array(),
            InputIndices = ar.ReadUInt16Array(),
            OutputIndices = ar.ReadUInt16Array(),
            FromValues = ar.ReadFloatArray(),
            ToValues = ar.ReadFloatArray(),
            SlopeValues = ar.ReadFloatArray(),
            CutValues = ar.ReadFloatArray(),
            InputCount = ar.ReadUInt16(),
            OutputCount = ar.ReadUInt16()
        };
    }
}

public sealed class RlRangeMap
{
    public RlRange[] Ranges = [];

    public static RlRangeMap Read(TerseBinaryArchive ar) => new() { Ranges = ar.ReadArray(() => RlRange.Read(ar)) };
}

public sealed class RlRange
{
    public float From;
    public float To;
    public ushort[] Rows = [];

    public static RlRange Read(TerseBinaryArchive ar) => new()
    {
        From = ar.ReadFloat(),
        To = ar.ReadFloat(),
        Rows = ar.ReadUInt16Array()
    };
}

public sealed class RlControlInitializer
{
    public uint Index;
    public float Value;

    public static RlControlInitializer Read(TerseBinaryArchive ar) => new()
    {
        Index = ar.ReadUInt32(),
        Value = ar.ReadFloat()
    };
}

public sealed class RlControls
{
    public ushort[][] RegisteredControls = [];
    public RlConditionalTable GuiToRawMapping = new();
    public RlControlInitializer[] InitialValues = [];

    public static RlControls Read(TerseBinaryArchive ar) => new()
    {
        RegisteredControls = ar.ReadMatrix(ar.ReadUInt16),
        GuiToRawMapping = RlConditionalTable.Read(ar),
        InitialValues = ar.ReadArray(() => RlControlInitializer.Read(ar))
    };
}

public sealed class RlPsd
{
    public ulong Offset;
    public ulong Size;
    public float Weight;

    public static RlPsd Read(TerseBinaryArchive ar) => new()
    {
        Offset = ar.ReadUInt64(),
        Size = ar.ReadUInt64(),
        Weight = ar.ReadFloat()
    };
}

public sealed class RlPsdNet
{
    public ushort[][] InputLods = [];
    public ushort[][] OutputLods = [];
    public ushort[] InputIndicesPerPsd = [];
    public RlPsd[] Psds = [];
    public ushort PsdMinIndex;
    public ushort PsdMaxIndex;

    public static RlPsdNet Read(TerseBinaryArchive ar) => new()
    {
        InputLods = ar.ReadMatrix(ar.ReadUInt16),
        OutputLods = ar.ReadMatrix(ar.ReadUInt16),
        InputIndicesPerPsd = ar.ReadUInt16Array(),
        Psds = ar.ReadArray(() => RlPsd.Read(ar)),
        PsdMinIndex = ar.ReadUInt16(),
        PsdMaxIndex = ar.ReadUInt16()
    };
}

public sealed class RlColumnLod
{
    public uint Size;
    public uint SizeAlignedTo4;
    public uint SizeAlignedTo8;

    public static RlColumnLod Read(TerseBinaryArchive ar) => new()
    {
        Size = ar.ReadUInt32(),
        SizeAlignedTo4 = ar.ReadUInt32(),
        SizeAlignedTo8 = ar.ReadUInt32()
    };
}

public sealed class RlPaddedBlockView
{
    public uint Size;
    public uint SizePaddedToLastFullBlock;
    public uint SizePaddedToSecondLastFullBlock;

    public static RlPaddedBlockView Read(TerseBinaryArchive ar) => new()
    {
        Size = ar.ReadUInt32(),
        SizePaddedToLastFullBlock = ar.ReadUInt32(),
        SizePaddedToSecondLastFullBlock = ar.ReadUInt32()
    };
}

public sealed class RlLodRegion
{
    public RlColumnLod InputLods = new();
    public RlPaddedBlockView OutputLods = new();

    public static RlLodRegion Read(TerseBinaryArchive ar) => new()
    {
        InputLods = RlColumnLod.Read(ar),
        OutputLods = RlPaddedBlockView.Read(ar)
    };
}

public sealed class RlBpcmJointGroup
{
    public uint ValuesOffset;
    public uint InputIndicesOffset;
    public uint OutputIndicesOffset;
    public uint LodsOffset;
    public uint OutputRotationIndicesOffset;
    public uint OutputRotationLodsOffset;
    public uint ValuesSize;
    public uint ColCount;
    public uint RowCount;

    public static RlBpcmJointGroup Read(TerseBinaryArchive ar) => new()
    {
        ValuesOffset = ar.ReadUInt32(),
        InputIndicesOffset = ar.ReadUInt32(),
        OutputIndicesOffset = ar.ReadUInt32(),
        LodsOffset = ar.ReadUInt32(),
        OutputRotationIndicesOffset = ar.ReadUInt32(),
        OutputRotationLodsOffset = ar.ReadUInt32(),
        ValuesSize = ar.ReadUInt32(),
        ColCount = ar.ReadUInt32(),
        RowCount = ar.ReadUInt32()
    };
}

public sealed class RlBpcmStorage
{
    public float[] Values = [];
    public ushort[] InputIndices = [];
    public ushort[] OutputIndices = [];
    public RlLodRegion[] LodRegions = [];
    public ushort[] OutputRotationIndices = [];
    public ushort[] OutputRotationLods = [];
    public RlBpcmJointGroup[] JointGroups = [];

    public static RlBpcmStorage Read(TerseBinaryArchive ar) => new()
    {
        Values = ar.ReadFloatArray(),
        InputIndices = ar.ReadUInt16Array(),
        OutputIndices = ar.ReadUInt16Array(),
        LodRegions = ar.ReadArray(() => RlLodRegion.Read(ar)),
        OutputRotationIndices = ar.ReadUInt16Array(),
        OutputRotationLods = ar.ReadUInt16Array(),
        JointGroups = ar.ReadArray(() => RlBpcmJointGroup.Read(ar))
    };
}

public sealed class RlJoints
{
    public RlBpcmStorage Bpcm = new();
    public float[] NeutralValues = [];
    public ushort[][] VariableAttributeIndices = [];
    public ushort[][] JointIndices = [];
    public ushort JointGroupCount;
}

public sealed class RlBlendShapes
{
    public ushort[] Lods = [];
    public ushort[] InputIndices = [];
    public ushort[] OutputIndices = [];

    public static RlBlendShapes Read(TerseBinaryArchive ar) => new()
    {
        Lods = ar.ReadUInt16Array(),
        InputIndices = ar.ReadUInt16Array(),
        OutputIndices = ar.ReadUInt16Array()
    };
}

public sealed class RlAnimatedMaps
{
    public ushort[] Lods = [];
    public RlConditionalTable Conditionals = new();

    public static RlAnimatedMaps Read(TerseBinaryArchive ar) => new()
    {
        Lods = ar.ReadUInt16Array(),
        Conditionals = RlConditionalTable.Read(ar)
    };
}

public sealed class RigLogicSnapshot
{
    public const uint RldsMagic = 0x524C4453;
    public const ushort ExpectedMajor = 13;
    public const ushort ExpectedMinor = 2;

    public bool HasRldsHeader;
    public ushort MajorVersion;
    public ushort MinorVersion;
    public RlConfiguration Configuration = new();
    public RlMetadata Metadata = new();
    public RlControls Controls = new();
    public ushort[] MeshRegionCounts = [];
    public RlPsdNet? PsdNet;
    public RlJoints Joints = new();
    public RlBlendShapes? BlendShapes;
    public RlAnimatedMaps? AnimatedMaps;

    public static RigLogicSnapshot Read(byte[] data, bool requireFullConsume = true)
        => Read(new TerseBinaryArchive(data), requireFullConsume);

    public static RigLogicSnapshot Read(TerseBinaryArchive ar, bool requireFullConsume = true)
    {
        var snapshot = new RigLogicSnapshot();
        if (ar.PeekMagicBe(RldsMagic))
        {
            snapshot.HasRldsHeader = true;
            ar.ReadUInt32();
            snapshot.MajorVersion = ar.ReadUInt16();
            snapshot.MinorVersion = ar.ReadUInt16();
            if (snapshot.MajorVersion != ExpectedMajor || snapshot.MinorVersion != ExpectedMinor)
                throw new InvalidDataException($"Unsupported RLDS version {snapshot.MajorVersion}.{snapshot.MinorVersion}; expected {ExpectedMajor}.{ExpectedMinor}");
        }
        else if (LooksLikeUnknownFrame(ar))
        {
            throw new InvalidDataException("Unsupported RigLogic snapshot framing; expected headerless Configuration or RLDS 13.2");
        }

        snapshot.Configuration = RlConfiguration.Read(ar);
        snapshot.Metadata = RlMetadata.Read(ar);

        var queue = new Queue<RlEvaluatorType>(snapshot.Metadata.Evaluators);
        RlEvaluatorType Pop() => queue.Count == 0 ? RlEvaluatorType.Null : queue.Dequeue();

        var mlType = Pop();
        var rbfType = Pop();
        var bpcmType = Pop();
        var quatType = Pop();
        var twistType = Pop();
        var mlJointType = Pop();
        var blendType = Pop();
        var animType = Pop();
        var psdType = Pop();

        snapshot.Controls = RlControls.Read(ar);

        if (mlType == RlEvaluatorType.Concrete)
            throw new NotSupportedException("Concrete machine-learned behavior snapshots are not supported");
        snapshot.MeshRegionCounts = ar.ReadUInt16Array();

        if (rbfType == RlEvaluatorType.Concrete)
            throw new NotSupportedException("Concrete RBF behavior snapshots are not supported");

        if (psdType == RlEvaluatorType.Concrete)
            snapshot.PsdNet = RlPsdNet.Read(ar);
        else if (psdType != RlEvaluatorType.Null && psdType != RlEvaluatorType.Auto)
            throw new NotSupportedException($"Unsupported PSD evaluator {psdType}");

        if (bpcmType == RlEvaluatorType.Concrete)
            snapshot.Joints.Bpcm = RlBpcmStorage.Read(ar);
        else if (bpcmType != RlEvaluatorType.Null)
            throw new NotSupportedException($"Unsupported BPCM evaluator {bpcmType}");

        if (quatType == RlEvaluatorType.Concrete)
            throw new NotSupportedException("Concrete quaternion joint evaluators are not supported");
        if (twistType == RlEvaluatorType.Concrete)
            throw new NotSupportedException("Concrete twist/swing evaluators are not supported");
        if (mlJointType == RlEvaluatorType.Concrete)
            throw new NotSupportedException("Concrete ML joint evaluators are not supported");

        snapshot.Joints.NeutralValues = ar.ReadFloatArray();
        snapshot.Joints.VariableAttributeIndices = ar.ReadMatrix(ar.ReadUInt16);
        snapshot.Joints.JointIndices = ar.ReadMatrix(ar.ReadUInt16);
        snapshot.Joints.JointGroupCount = ar.ReadUInt16();

        if (blendType == RlEvaluatorType.Concrete)
            snapshot.BlendShapes = RlBlendShapes.Read(ar);
        if (animType == RlEvaluatorType.Concrete)
            snapshot.AnimatedMaps = RlAnimatedMaps.Read(ar);

        if (requireFullConsume)
            ar.EnsureConsumed();
        return snapshot;
    }

    private static bool LooksLikeUnknownFrame(TerseBinaryArchive ar)
    {
        if (ar.Remaining < 4)
            return false;
        var pos = ar.Position;
        var b0 = ar.ReadUInt8();
        var b1 = ar.ReadUInt8();
        var b2 = ar.ReadUInt8();
        var b3 = ar.ReadUInt8();
        ar.Position = pos;
        return IsAsciiLetter(b0) && IsAsciiLetter(b1) && IsAsciiLetter(b2) && IsAsciiLetter(b3);
    }

    private static bool IsAsciiLetter(byte value) => value is (>= (byte) 'A' and <= (byte) 'Z') or (>= (byte) 'a' and <= (byte) 'z');
}
