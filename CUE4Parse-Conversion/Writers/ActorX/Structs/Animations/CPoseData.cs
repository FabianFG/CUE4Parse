using System.Collections.Generic;

namespace CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;

public class CPoseData
{
    public string PoseName;
    public List<CPoseKey> Keys = [];
    public float[] CurveData;
}
