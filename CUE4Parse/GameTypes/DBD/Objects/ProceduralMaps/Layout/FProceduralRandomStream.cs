using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout;

internal struct FProceduralRandomStream
{
    private uint _seed;

    public FProceduralRandomStream(int seed, EProceduralRandomStream stream) : this(seed, (int) stream) { }
    public FProceduralRandomStream(int seed, EGameplayElementType type) : this(seed, (int) EProceduralRandomStream.GameplayPopulation + (int) type) { }

    // DBD's stream initialization
    private FProceduralRandomStream(int seed, int streamIndex)
    {
        _seed = (uint) seed;
        for (var step = 0; step < streamIndex * 200; step++)
        {
            NextUInt32();
        }
    }

    // Get the value and advance the stream
    public uint NextUInt32() => _seed = unchecked(196314165u * _seed + 907633515u);
    public float NextFloat() => BitConverter.UInt32BitsToSingle(0x3F800000u | NextUInt32() >> 9) - 1.0f;
}
