using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class FProperty : FField
    {
        public int ArrayDim;
        public int ElementSize;
        public ulong PropertyFlags;
        public ushort RepIndex;
        public FName RepNotifyFunc;
        [JsonConverter(typeof(StringEnumConverter))]
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
    }

    public class FArrayProperty : FProperty
    {
        public FProperty? Inner;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            Inner = (FProperty?) SerializeSingleField(Ar);
        }
    }

    public class FBoolProperty : FProperty
    {
        public byte FieldSize;
        public byte ByteOffset;
        public byte ByteMask;
        public byte FieldMask;
        public byte BoolSize;
        public byte NativeBool;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            FieldSize = Ar.Read<byte>();
            ByteOffset = Ar.Read<byte>();
            ByteMask = Ar.Read<byte>();
            FieldMask = Ar.Read<byte>();
            BoolSize = Ar.Read<byte>();
            NativeBool = Ar.Read<byte>();
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
    }

    public class FClassProperty : FObjectProperty
    {
        public FPackageIndex MetaClass;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            MetaClass = new FPackageIndex(Ar);
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
    }

    public class FMulticastDelegateProperty : FProperty
    {
        public FPackageIndex SignatureFunction;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            SignatureFunction = new FPackageIndex(Ar);
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
    }

    public class FSoftClassProperty : FObjectProperty
    {
        public FPackageIndex MetaClass;

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
            MetaClass = new FPackageIndex(Ar);
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
    }

    public class FTextProperty : FProperty { }

    public class FUInt16Property : FNumericProperty { }

    public class FUInt32Property : FNumericProperty { }

    public class FUInt64Property : FNumericProperty { }
}