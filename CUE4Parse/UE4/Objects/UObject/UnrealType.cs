using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class FProperty : FField
    {
        public int ArrayDim;
        public int ElementSize;
        public ulong PropertyFlags;
        public ushort RepIndex;
        public FName RepNotifyFunc;
        public ELifetimeCondition BlueprintReplicationCondition;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            ArrayDim = Ar.Read<int>();
            ElementSize = Ar.Read<int>();
            PropertyFlags = Ar.Read<ulong>();
            RepIndex = Ar.Read<ushort>();
            RepNotifyFunc = Ar.ReadFName();
            BlueprintReplicationCondition = (ELifetimeCondition) Ar.Read<byte>();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (ArrayDim != 1)
            {
                writer.WritePropertyName("ArrayDim");
                writer.WriteValue(ArrayDim);
            }

            if (ElementSize != 0)
            {
                writer.WritePropertyName("ElementSize");
                writer.WriteValue(ElementSize);
            }

            if (PropertyFlags != 0)
            {
                writer.WritePropertyName("PropertyFlags");
                writer.WriteValue(PropertyFlags);
            }

            if (RepIndex != 0)
            {
                writer.WritePropertyName("RepIndex");
                writer.WriteValue(RepIndex);
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

    public class FArrayProperty : FProperty
    {
        public FProperty? Inner;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            Inner = (FProperty?) SerializeSingleField(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Inner");
            serializer.Serialize(writer, Inner);
        }
    }

    public class FBoolProperty : FProperty
    {
        public byte FieldSize;
        public byte ByteOffset;
        public byte ByteMask;
        public byte FieldMask;
        public byte BoolSize;
        public bool bIsNativeBool;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            FieldSize = Ar.Read<byte>();
            ByteOffset = Ar.Read<byte>();
            ByteMask = Ar.Read<byte>();
            FieldMask = Ar.Read<byte>();
            BoolSize = Ar.Read<byte>();
            bIsNativeBool = Ar.ReadFlag();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("FieldSize");
            writer.WriteValue(FieldSize);

            writer.WritePropertyName("ByteOffset");
            writer.WriteValue(ByteOffset);

            writer.WritePropertyName("ByteMask");
            writer.WriteValue(ByteMask);

            writer.WritePropertyName("FieldMask");
            writer.WriteValue(FieldMask);

            writer.WritePropertyName("BoolSize");
            writer.WriteValue(BoolSize);

            writer.WritePropertyName("bIsNativeBool");
            writer.WriteValue(bIsNativeBool);
        }
    }

    public class FByteProperty : FNumericProperty
    {
        public FPackageIndex Enum;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            Enum = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Enum");
            serializer.Serialize(writer, Enum);
        }
    }

    public class FClassProperty : FObjectProperty
    {
        public FPackageIndex MetaClass;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            MetaClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("MetaClass");
            serializer.Serialize(writer, MetaClass);
        }
    }

    public class FDelegateProperty : FProperty
    {
        public FPackageIndex SignatureFunction;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            SignatureFunction = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("SignatureFunction");
            serializer.Serialize(writer, SignatureFunction);
        }
    }

    public class FEnumProperty : FProperty
    {
        public FNumericProperty? UnderlyingProp;
        public FPackageIndex Enum;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            Enum = new FPackageIndex(Ar);
            UnderlyingProp = (FNumericProperty?) SerializeSingleField(Ar);
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

    public class FFieldPathProperty : FProperty
    {
        public FName PropertyClass;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            PropertyClass = Ar.ReadFName();
        }
    }

    public class FFloatProperty : FNumericProperty { }

    public class FInt16Property : FNumericProperty { }

    public class FInt64Property : FNumericProperty { }

    public class FInt8Property : FNumericProperty { }

    public class FIntProperty : FNumericProperty { }

    public class FInterfaceProperty : FProperty
    {
        public FPackageIndex InterfaceClass;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            InterfaceClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("InterfaceClass");
            serializer.Serialize(writer, InterfaceClass);
        }
    }

    public class FMapProperty : FProperty
    {
        public FProperty? KeyProp;
        public FProperty? ValueProp;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            KeyProp = (FProperty?) SerializeSingleField(Ar);
            ValueProp = (FProperty?) SerializeSingleField(Ar);
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

    public class FMulticastDelegateProperty : FProperty
    {
        public FPackageIndex SignatureFunction;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            SignatureFunction = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("SignatureFunction");
            serializer.Serialize(writer, SignatureFunction);
        }
    }

    public class FMulticastInlineDelegateProperty : FProperty
    {
        public FPackageIndex SignatureFunction;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            SignatureFunction = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("SignatureFunction");
            serializer.Serialize(writer, SignatureFunction);
        }
    }

    public class FNameProperty : FProperty { }

    public class FNumericProperty : FProperty { }

    public class FObjectProperty : FProperty
    {
        public FPackageIndex PropertyClass;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            PropertyClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("PropertyClass");
            serializer.Serialize(writer, PropertyClass);
        }
    }

    public class FSoftClassProperty : FObjectProperty
    {
        public FPackageIndex MetaClass;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            MetaClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("MetaClass");
            serializer.Serialize(writer, MetaClass);
        }
    }

    public class FSoftObjectProperty : FObjectProperty { }

    public class FSetProperty : FProperty
    {
        public FProperty? ElementProp;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            ElementProp = (FProperty?) SerializeSingleField(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("ElementProp");
            serializer.Serialize(writer, ElementProp);
        }
    }

    public class FStrProperty : FProperty { }

    public class FStructProperty : FProperty
    {
        public FPackageIndex Struct;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            Struct = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Struct");
            serializer.Serialize(writer, Struct);
        }
    }

    public class FTextProperty : FProperty { }

    public class FUInt16Property : FNumericProperty { }

    public class FUInt32Property : FNumericProperty { }

    public class FUInt64Property : FNumericProperty { }
}