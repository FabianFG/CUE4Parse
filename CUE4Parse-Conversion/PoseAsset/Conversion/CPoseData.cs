using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.PoseAsset.Conversion;

public class CPoseData
{
    public string PoseName;
    public List<CPoseKey> Keys = [];
    public float[] CurveData;
}