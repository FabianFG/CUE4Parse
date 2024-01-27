using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUE4Parse.UE4.Wwise.Enums
{
    public enum EPosition3DPlayType : uint
    {
        SequenceStep,
        RandomStep,
        SequenceContinuous,
        RandomContinuous,
        SequenceStepNewPath,
        RandomStepNewPath,
    }
}
