using System;

namespace CUE4Parse.UE4.CriWare.Decoders.HCA;

internal static class JaggedArray
{
    public static T CreateJaggedArray<T>(params int[] lengths)
    {
        Type? elementType = typeof(T).GetElementType() ?? throw new Exception("Type has no element type.");

        return (T) InitJaggedArray(elementType, lengths, 0);
    }

    private static object InitJaggedArray(Type type, int[] lengths, int arrayIndex)
    {
        Array array = Array.CreateInstance(type, lengths[arrayIndex]);
        Type? subElementType = type.GetElementType();

        if (type.HasElementType && subElementType != null)
        {
            for (int i = 0; i < lengths[arrayIndex]; i++)
            {
                array.SetValue(InitJaggedArray(subElementType, lengths, arrayIndex + 1), i);
            }
        }

        return array;
    }
}
