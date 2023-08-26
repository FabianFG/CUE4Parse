using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class UProperty : UField
    {
        public int ArrayDim;
        public EPropertyFlags PropertyFlags;
        public FName RepNotifyFunc;
        public ELifetimeCondition BlueprintReplicationCondition;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            ArrayDim = Ar.Read<int>();
            PropertyFlags = Ar.Read<EPropertyFlags>();
            RepNotifyFunc = Ar.ReadFName();
            if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.PropertiesSerializeRepCondition)
            {
                BlueprintReplicationCondition = (ELifetimeCondition) Ar.Read<byte>();
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (ArrayDim != 1)
            {
                writer.WritePropertyName("ArrayDim");
                writer.WriteValue(ArrayDim);
            }

            if (PropertyFlags != 0)
            {
                writer.WritePropertyName("PropertyFlags");
                writer.WriteValue(PropertyFlags.ToStringBitfield());
            }

            if (!RepNotifyFunc.IsNone)
            {
                writer.WritePropertyName("RepNotifyFunc");
                serializer.Serialize(writer, RepNotifyFunc);
            }

            if (BlueprintReplicationCondition != ELifetimeCondition.COND_None)
            {
                writer.WritePropertyName("BlueprintReplicationCondition");
                writer.WriteValue(BlueprintReplicationCondition.ToString());
            }
        }
    }

    public class UNumericProperty : UProperty { }

    public class UByteProperty : UNumericProperty
    {
        public FPackageIndex Enum; // UEnum

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Enum = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Enum");
            serializer.Serialize(writer, Enum);
        }
    }

    public class UInt8Property : UNumericProperty { }

    public class UIntProperty : UNumericProperty { }

    public class UUInt16Property : UNumericProperty { }

    public class UUInt32Property : UNumericProperty { }

    public class UUInt64Property : UNumericProperty { }

    public class UFloatProperty : UNumericProperty { }

    public class UDoubleProperty : UNumericProperty { }

    public class UBoolProperty : UProperty
    {
        public byte BoolSize;
        public bool bIsNativeBool;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            BoolSize = Ar.Read<byte>();
            bIsNativeBool = Ar.ReadFlag();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("BoolSize");
            writer.WriteValue(BoolSize);

            writer.WritePropertyName("bIsNativeBool");
            writer.WriteValue(bIsNativeBool);
        }
    }

    public class UObjectPropertyBase : UProperty
    {
        public FPackageIndex PropertyClass; // UClass

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            PropertyClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("PropertyClass");
            serializer.Serialize(writer, PropertyClass);
        }
    }

    public class UObjectProperty : UObjectPropertyBase { }

    public class UWeakObjectProperty : UObjectPropertyBase { }

    public class ULazyObjectProperty : UObjectPropertyBase { }

    public class USoftObjectProperty : UObjectPropertyBase { }

    public class UAssetObjectProperty : UObjectPropertyBase { }

    public class UAssetClassProperty : UObjectPropertyBase
    {
        public FPackageIndex MetaClass; // UClass

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            MetaClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("MetaClass");
            serializer.Serialize(writer, MetaClass);
        }
    }

    public class UClassProperty : UObjectProperty
    {
        public FPackageIndex MetaClass; // UClass

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            MetaClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("MetaClass");
            serializer.Serialize(writer, MetaClass);
        }
    }

    public class USoftClassProperty : UObjectPropertyBase
    {
        public FPackageIndex MetaClass; // UClass

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            MetaClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("MetaClass");
            serializer.Serialize(writer, MetaClass);
        }
    }

    public class UInterfaceProperty : UProperty
    {
        public FPackageIndex InterfaceClass; // UClass

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            InterfaceClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("InterfaceClass");
            serializer.Serialize(writer, InterfaceClass);
        }
    }

    public class UNameProperty : UProperty { }

    public class UStrProperty : UProperty { }

    public class UArrayProperty : UProperty
    {
        public FPackageIndex Inner; // UProperty

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Inner = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Inner");
            serializer.Serialize(writer, Inner);
        }
    }

    public class UMapProperty : UProperty
    {
        public FPackageIndex KeyProp; // UProperty
        public FPackageIndex ValueProp; // UProperty

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            KeyProp = new FPackageIndex(Ar);
            ValueProp = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("KeyProp");
            serializer.Serialize(writer, KeyProp);

            writer.WritePropertyName("ValueProp");
            serializer.Serialize(writer, ValueProp);
        }
    }

    public class USetProperty : UProperty
    {
        public FPackageIndex ElementProp; // UProperty

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            ElementProp = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("ElementProp");
            serializer.Serialize(writer, ElementProp);
        }
    }

    public class UStructProperty : UProperty
    {
        public FPackageIndex Struct; // UScriptStruct

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Struct = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Struct");
            serializer.Serialize(writer, Struct);
        }
    }

    public class UDelegateProperty : UProperty
    {
        public FPackageIndex SignatureFunction; // UFunction

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            SignatureFunction = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("SignatureFunction");
            serializer.Serialize(writer, SignatureFunction);
        }
    }

    public class UMulticastDelegateProperty : UProperty
    {
        public FPackageIndex SignatureFunction; // UFunction

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            SignatureFunction = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("SignatureFunction");
            serializer.Serialize(writer, SignatureFunction);
        }
    }

    public class UMulticastInlineDelegateProperty : UMulticastDelegateProperty { }

    public class UMulticastSparseDelegateProperty : UMulticastDelegateProperty { }

    public class UEnumProperty : UProperty
    {
        public FPackageIndex UnderlyingProp; // UNumericProperty
        public FPackageIndex Enum; // UEnum

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Enum = new FPackageIndex(Ar);
            UnderlyingProp = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Enum");
            serializer.Serialize(writer, Enum);

            writer.WritePropertyName("UnderlyingProp");
            serializer.Serialize(writer, UnderlyingProp);
        }
    }

    public class UTextProperty : UProperty { }
}
