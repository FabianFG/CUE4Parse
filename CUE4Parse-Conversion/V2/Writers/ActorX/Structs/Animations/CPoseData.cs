using System.Collections.Generic;

namespace CUE4Parse_Conversion.V2.Writers.ActorX.Structs.Animations;

public class CPoseData
{
    public string PoseName;
    public List<CPoseKey> Keys = [];
    public float[] CurveData;
}
