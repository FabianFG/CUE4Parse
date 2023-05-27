using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject
{
    [SkipObjectRegistration]
    public class UStruct : UField
    {
        public FPackageIndex SuperStruct;
        public FPackageIndex[] Children;
        public FField[] ChildProperties;
        public KismetExpression[] ScriptBytecode;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            SuperStruct = new FPackageIndex(Ar);
            if (FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.RemoveUField_Next)
            {
                var firstChild = new FPackageIndex(Ar);
                Children = firstChild.IsNull ? Array.Empty<FPackageIndex>() : new[] { firstChild };
            }
            else
            {
                Children = Ar.ReadArray(() => new FPackageIndex(Ar));
            }

            if (FCoreObjectVersion.Get(Ar) >= FCoreObjectVersion.Type.FProperties)
            {
                DeserializeProperties(Ar);
            }

            var bytecodeBufferSize = Ar.Read<int>();
            var serializedScriptSize = Ar.Read<int>();

            if (Ar.Owner.Provider?.ReadScriptData == true && serializedScriptSize > 0)
            {
                using var kismetAr = new FKismetArchive(Name, Ar.ReadBytes(serializedScriptSize), Ar.Owner, Ar.Versions);

                try
                {
                    var tempCode = new List<KismetExpression>();
                    while (kismetAr.Position < kismetAr.Length)
                    {
                        tempCode.Add(kismetAr.ReadExpression());
                    }
                    ScriptBytecode = tempCode.ToArray();
                }
                catch (Exception e)
                {
                    Log.Warning(e, $"Failed to serialize script bytecode in {Name}");
                }
            }
            else
            {
                Ar.Position += serializedScriptSize;
            }
        }

        protected void DeserializeProperties(FAssetArchive Ar)
        {
            ChildProperties = Ar.ReadArray(() =>
            {
                var propertyTypeName = Ar.ReadFName();
                var prop = FField.Construct(propertyTypeName);
                prop.Deserialize(Ar);
                return prop;
            });
        }

        // ignore inner properties and return main one
        public bool GetProperty(FName name, out FField? property) 
        {
            property = null;
            if (ChildProperties is null) return false;

            foreach (var item in ChildProperties)
            {
                if (item.Name.Text == name.Text)
                {
                    property = item;
                    return true;
                }
            }

            return false;
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (SuperStruct is { IsNull: false })
            {
                writer.WritePropertyName("SuperStruct");
                serializer.Serialize(writer, SuperStruct);
            }

            if (Children is { Length: > 0 })
            {
                writer.WritePropertyName("Children");
                serializer.Serialize(writer, Children);
            }

            if (ChildProperties is { Length: > 0 })
            {
                writer.WritePropertyName("ChildProperties");
                serializer.Serialize(writer, ChildProperties);
            }

            if (ScriptBytecode is { Length: > 0 })
            {
                writer.WritePropertyName("ScriptBytecode");
                writer.WriteStartArray();

                foreach (var expr in ScriptBytecode)
                {
                    writer.WriteStartObject();
                    expr.WriteJson(writer, serializer, true);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }
    }
}
