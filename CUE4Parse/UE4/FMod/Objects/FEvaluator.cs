using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.FMod.Enums;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FEvaluator
{
    public readonly EEvaluatorType Type;
    public readonly object? Data;

    public FEvaluator(BinaryReader Ar)
    {
        uint rawType = Ar.ReadUInt32();
        Type = (EEvaluatorType)(rawType & 0xFF); // Only lower 8 bits are used for the type
        Data = null;

        switch (Type)
        {
            case EEvaluatorType.Basic0:
            case EEvaluatorType.Basic1:
            case EEvaluatorType.Basic2:
            case EEvaluatorType.Basic3:
            case EEvaluatorType.Type12:
            case EEvaluatorType.Type30:
                break;

            case EEvaluatorType.Type10:
                Data = Ar.ReadUInt32();
                break;

            case EEvaluatorType.Type11:
                Data = new FModGuid(Ar);
                break;

            case EEvaluatorType.Type20:
                Data = new float[]
                {
                        Ar.ReadSingle(),
                        Ar.ReadSingle()
                }; ;
                break;

            default:
                throw new InvalidDataException($"Unknown evaluator type: {Type} (Raw: {rawType})");
        }
    }

    #region Readers
    public static List<FEvaluator> ReadEvaluatorList(BinaryReader Ar)
    {
        var evaluators = new List<FEvaluator>();

        var totalSize = Ar.ReadInt32();
        if (totalSize <= 0) return [];

        var startPos = Ar.BaseStream.Position;
        var endPos = startPos + totalSize;
        while (Ar.BaseStream.Position < endPos)
        {
            evaluators.Add(new FEvaluator(Ar));
        }

        return evaluators;
    }

    #endregion
}
