using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font;

public class UFontFace : UObject
{
    public FFontFaceData? FontFaceData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bCooked = false;
        if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.AddedCookedBoolFontFaceAssets)
        {
            bCooked = Ar.ReadBoolean(); 
        }

        var bSaveInlineData = Ar.ReadBoolean();
        if (bSaveInlineData)
        {
            FontFaceData = new FFontFaceData(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        // if (FontFaceData == null) return;
        // writer.WritePropertyName("FontFaceData");
        // serializer.Serialize(writer, FontFaceData);
    }
}

public class FFontFaceData
{
    public byte[] Data;
    public FPreprocessedFontGeometry[]? PreprocessedFontGeometries;

    public FFontFaceData(FArchive Ar)
    {
        Data = Ar.ReadArray<byte>();

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.AddedPreprocessedFontGeometry)
        {
            PreprocessedFontGeometries = Ar.ReadArray(() => new FPreprocessedFontGeometry(Ar));
        }
    }
}

public class FPreprocessedFontGeometry(FArchive Ar)
{
    public bool GlobalWindingReversal = Ar.ReadBoolean();
    public Dictionary<int, FGlyphHeader> Glyphs = Ar.ReadMap(Ar.Read<int>, Ar.Read<FGlyphHeader>);
    public byte[] ContourData = Ar.ReadArray<byte>();
    public short[] CoordinateData = Ar.ReadArray<short>();
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FGlyphHeader
{
    /** Glyph flags - see constants in FPreprocessedGlyphGeometry */
    byte Flags;
    /** Number of glyph's contours */
    int ContourCount;
    /** Initial index of the glyph's contour data within the ContourData array */
    int ContourDataStart;
    /** Number of elements of the ContourData array for this glyph */
    int ContourDataLength;
    /** Initial index of the glyph's coordinates within the CoordinateData array */
    int CoordinateDataStart;
    /** Number of elements of the CoordinateData array for this glyph */
    int CoordinateDataLength;
}
