using System.Collections.Generic;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.CurveExpression;

public class FExpressionObject
{
    public List<OpElement> Expression = [];
    
    public FExpressionObject(FArchive Ar)
    {
        var operandCount = Ar.Read<int>();
        for (var operandIndex = 0; operandIndex < operandCount; operandIndex++)
        {
            var operandType = Ar.Read<int>();
            switch (operandType)
            {
                case OpElement.EOperator: 
                {
                    var operatorType = Ar.Read<int>();
                    Expression.Add(new OpElement<EOperator>((EOperator) operatorType));
                    break;
                }
                case OpElement.FName:
                {
                    Expression.Add(new OpElement<FName>(Ar.ReadFName()));
                    break;
                }
                case OpElement.FFunctionRef:
                {
                    var functionIndex = Ar.Read<int>();
                    Expression.Add(new OpElement<FFunctionRef>(new FFunctionRef(functionIndex)));
                    break;
                }
                case OpElement.Float:
                {
                    var value = Ar.Read<float>();
                    Expression.Add(new OpElement<float>(value));
                    break;
                }
                default:
                {
                    throw new ParserException($"Invalid operand type: {operandType}");
                }
            }
        }
    }
}

public enum EOperator : int
{
    Negate,				// Negation operator.
    Add,				// Add last two values on stack and add the result to the stack.
    Subtract,			// Same but subtract 
    Multiply,			// Same but multiply
    Divide,				// Same but divide (div-by-zero returns zero)
    Modulo,				// Same but modulo (mod-by-zero returns zero)
    Power,				// Raise to power
    FloorDivide,    	// Divide and round the result down
}

public struct FFunctionRef
{
    public int Index;
    
    public FFunctionRef(int index)
    {
        Index = index;
    }
}

public class OpElement
{
    public const int EOperator = 0;
    public const int FName = 1;
    public const int FFunctionRef = 2;
    public const int Float = 3;

    public bool TryGet<T>(out T outValue)
    {
        if (this is OpElement<T> op)
        {
            outValue = op.Value;
            return true;
        }

        outValue = default;
        return false;
    }
}

public class OpElement<T> : OpElement
{
    public T Value;

    public OpElement(T value)
    {
        Value = value;
    }
}