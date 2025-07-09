﻿using System;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(BoolPropertyConverter))]
    public class BoolProperty : FPropertyTagType<bool>
    {
        public BoolProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
        {
            switch (type)
            {
                case ReadType.NORMAL when !Ar.HasUnversionedProperties:
                    Value = tagData?.Bool == true;
                    break;
                case ReadType.NORMAL:
                case ReadType.MAP:
                case ReadType.ARRAY:
                case ReadType.OPTIONAL:
                    Value = Ar.ReadFlag();
                    break;
                case ReadType.ZERO:
                    Value = tagData?.Bool == true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}