using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class UScriptMap
    {
        public Dictionary<FPropertyTagType?, FPropertyTagType?> Properties;

        public UScriptMap(FAssetArchive Ar, FPropertyTagData.MapProperty tagData)
        {
            int numKeyToRemove = Ar.Read<int>();
            for (int i = 0; i < numKeyToRemove; i++)
            {
                FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType.Text, tagData, ReadType.MAP);
            }

            int numEntries = Ar.Read<int>();
            Properties = new Dictionary<FPropertyTagType?, FPropertyTagType?>(numEntries);
            for (int i = 0; i < Properties.Count; i++)
            {
                try
                {
                    Properties[FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType.Text, tagData, ReadType.MAP)] = FPropertyTagType.ReadPropertyTagType(Ar, tagData.ValueType.Text, tagData, ReadType.MAP);
                }
                catch (ParserException e)
                {
                    throw new ParserException(Ar, $"Failed to read key/value pair for index {i} in map", e);
                }
            }
        }
    }
}
