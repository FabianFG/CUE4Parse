using System;
using System.Collections.Generic;
using System.Data;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Animation.CurveExpression;

public class FExpressionEvaluator
{
    public float Execute(FExpressionObject inExpressionObject, Func<FName, float> inConstantEvaluator)
    {
        var valueStack = new List<float>();

        foreach (var token in inExpressionObject.Expression)
        {
            if (token.TryGet<float>(out var value))
            {
                valueStack.Add(value);
            }
            else if (token.TryGet<FName>(out var constantName))
            {
                var constantValue = inConstantEvaluator(constantName);
                valueStack.Add(constantValue);
            }
            else if (token.TryGet<EOperator>(out var op))
            {
                switch (op)
                {
                    case EOperator.Negate:
                    {
                        valueStack[^1] = -valueStack[^1];
                        break;
                    }
                    case EOperator.Add:
                    {
                        var v = valueStack.Pop();
                        valueStack[^1] += v;
                        break;
                    }
                    case EOperator.Subtract:
                    {
                        var v = valueStack.Pop();
                        valueStack[^1] -= v;
                        break;
                    }
                    case EOperator.Multiply:
                    {
                        var v = valueStack.Pop();
                        valueStack[^1] *= v;
                        break;
                    }
                    case EOperator.Divide:
                    {
                        var v = valueStack.Pop();
                        if (UnrealMath.IsNearlyZero(v))
                        {
                            valueStack[^1] = 0.0f;
                        }
                        else
                        {
                            valueStack[^1] /= v;
                        }
                        break;
                    }
                    case EOperator.Modulo:
                    {
                        var v = valueStack.Pop();
                        if (UnrealMath.IsNearlyZero(v))
                        {
                            valueStack[^1] = 0.0f;
                        }
                        else
                        {
                            valueStack[^1] %= v;
                        }
                        break;
                    }
                    case EOperator.Power:
                    {
                        var v = valueStack.Pop();
                        valueStack[^1] = MathF.Pow(valueStack[^1], v);
                        if (!float.IsFinite(valueStack[^1]))
                        {
                            valueStack[^1] = 0.0f;
                        }
                        break;
                    }
                    case EOperator.FloorDivide:
                    {
                        var v = valueStack.Pop();
                        if (UnrealMath.IsNearlyZero(v))
                        {
                            valueStack[^1] = 0.0f;
                        }
                        else
                        {
                            valueStack[^1] /= v;
                        }

                        valueStack[^1] = MathF.Floor(valueStack[^1]);
                        break;
                    }
                }
            }
            else if (token.TryGet<FFunctionRef>(out var funcRef))
            {
                if (!GBuiltInFunctions.IsValidFunctionIndex(funcRef.Index))
                {
                    return 0.0f;
                }

                var functionInfo = GBuiltInFunctions.GetInfoByIndex(funcRef.Index);
                if (functionInfo.ArgumentCount <= valueStack.Count)
                {
                    throw new EvaluateException($"Stack does not have enough data to supply function with {functionInfo.ArgumentCount} arguments");
                }

                var arguments = new float[functionInfo.ArgumentCount];
                for (var index = 0; index < functionInfo.ArgumentCount; index++)
                {
                    arguments[index] = valueStack.Pop();
                }
                
                valueStack.Add(functionInfo.Function(arguments));
            }
        }

        return valueStack[^1];
    }
}

public static class GBuiltInFunctions
{
    private static Dictionary<string, int> FunctionNameIndex = [];
    private static List<FFunctionInfo> Functions = [];


    static GBuiltInFunctions()
    {
        CE_EXPR("clamp", 3, args => Math.Clamp(args[0], args[1], args[2]));
        CE_EXPR("min", 2, args => Math.Min(args[0], args[1]));
        CE_EXPR("max", 2, args => Math.Max(args[0], args[1]));

        CE_EXPR("abs", 1, args => Math.Abs(args[0]));
        CE_EXPR("round", 1, args => MathF.Round(args[0]));
        CE_EXPR("ceil", 1, args => MathF.Ceiling(args[0]));
        CE_EXPR("floor", 1, args => MathF.Floor(args[0]));

        CE_EXPR("sin", 1, args => MathF.Sin(args[0]));
        CE_EXPR("cos", 1, args => MathF.Cos(args[0]));
        CE_EXPR("tan", 1, args => MathF.Tan(args[0]));
        CE_EXPR("asin", 1, args => MathF.Asin(args[0]));
        CE_EXPR("acos", 1, args => MathF.Acos(args[0]));
        CE_EXPR("atan", 1, args => MathF.Atan(args[0]));

        CE_EXPR("sqrt", 1, args => MathF.Sqrt(args[0]));
        CE_EXPR("isqrt", 1, args => 1 / MathF.Sqrt(args[0]));
        
        CE_EXPR("pi", 0, _ => MathF.PI);
        CE_EXPR("e", 0, _ => MathF.E);
        CE_EXPR("undef", 0, _ => float.NaN);
    }

    public static int FindByName(string inName)
    {
        if (FunctionNameIndex.TryGetValue(inName, out var index))
        {
            return index;
        }

        return -1;
    }

    public static bool IsValidFunctionIndex(int index)
    {
        return index >= 0 && index < Functions.Count;
    }

    public static FFunctionInfo GetInfoByIndex(int inIndex)
    {
        return Functions[inIndex];
    }

    private static void CE_EXPR(string name, int arguments, Func<float[], float> function)
    {
        FunctionNameIndex[name] = Functions.Count;
        Functions.Add(new FFunctionInfo(arguments, function));
    }
}

public class FFunctionInfo
{
    public int ArgumentCount;
    public Func<float[], float> Function;

    public FFunctionInfo(int argumentCount, Func<float[], float> function)
    {
        ArgumentCount = argumentCount;
        Function = function;
    }
}
