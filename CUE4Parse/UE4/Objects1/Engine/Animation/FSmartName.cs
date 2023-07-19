using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Animation
{
    //[JsonConverter(typeof(FSmartNameConverter))] For consistency with the property serialization structure
    public readonly struct FSmartName : IUStruct
    {
        public readonly FName DisplayName;

        public FSmartName(FArchive Ar)
        {
            DisplayName = Ar.ReadFName();
            if (FAnimPhysObjectVersion.Get(Ar) < FAnimPhysObjectVersion.Type.RemoveUIDFromSmartNameSerialize)
            {
                Ar.Read<ushort>(); // TempUID
            }

            // only save if it's editor build and not cooking
            if (FAnimPhysObjectVersion.Get(Ar) < FAnimPhysObjectVersion.Type.SmartNameRefactorForDeterministicCooking)
            {
                Ar.Read<FGuid>(); // TempGUID
            }
        }

        public FSmartName(FStructFallback data)
        {
            DisplayName = data.GetOrDefault<FName>(nameof(DisplayName));
        }
    }

    public class FSmartNameConverter : JsonConverter<FSmartName>
    {
        public override void WriteJson(JsonWriter writer, FSmartName value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.DisplayName);
        }

        public override FSmartName ReadJson(JsonReader reader, Type objectType, FSmartName existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
