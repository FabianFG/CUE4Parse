using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using System;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class UScriptArray
    {
        public readonly string InnerType; 
        public readonly FPropertyTag? InnerTag;
        public readonly List<FPropertyTagType> Properties;

        public UScriptArray(FAssetArchive Ar, string innerType)
        {
            InnerType = innerType;
            var elementCount = Ar.Read<int>();
            if (innerType == "StructProperty" || innerType == "ArrayProperty")
            {
                InnerTag = new FPropertyTag(Ar, false);
                if (InnerTag == null)
                    throw new ParserException($"Couldn't read ArrayProperty with inner type {innerType}");
            }

            Properties = new List<FPropertyTagType>(elementCount);
            var innerTagData = InnerTag?.TagData;
            for (int i = 0; i < elementCount; i++)
            {
                var property = FPropertyTagType.ReadPropertyTagType(Ar, innerType, innerTagData, ReadType.ARRAY);
                if (property != null)
                    Properties.Add(property);
                else
                    Console.WriteLine(
                        $"Failed to read array property of type {innerType} at ${Ar.Position}, index {i}");
            }
        }

        public override string ToString() => $"{InnerType}[{Properties.Count}]";
    }
}
