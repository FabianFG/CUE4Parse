using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.StructUtils;

public enum EPropertyBagPropertyType : byte
{
    None,
    Bool,
    Byte,
    Int32,
    Int64,
    Float,
    Double,
    Name,
    String,
    Text,
    Enum,
    Struct,
    Object,
    SoftObject,
    Class,
    SoftClass,
    UInt32,	// Type not fully supported at UI, will work with restrictions to type editing
    UInt64, // Type not fully supported at UI, will work with restrictions to type editing

    Count
};

public enum EPropertyBagContainerType : byte
{
    None,
    Array,
    Count
};

public class FInstancedPropertyBag : IUStruct
{
    public FPropertyBagPropertyDesc[] PropertyDescs;
    public int SerialSize;

    public FInstancedPropertyBag(FAssetArchive Ar)
    {
        var Version = EVersion.LatestVersion;
        if (FPropertyBagCustomVersion.Get(Ar) < FPropertyBagCustomVersion.Type.ContainerTypes)
        {
            Version = Ar.Read<EVersion>();
        }

        var bHasData = Ar.ReadBoolean();
        if (!bHasData)
            return;

        PropertyDescs = Ar.ReadArray(() => new FPropertyBagPropertyDesc(Ar));

        if (Version >= EVersion.SerializeStructSize)
            SerialSize = Ar.Read<int>();

        // TODO implement
        // BagStruct = const_cast<UPropertyBag*>(UPropertyBag::GetOrCreateFromDescs(PropertyDescs));
        // and then read as FStructFallback

        Ar.Position += SerialSize;
    }

    public enum EVersion : byte
    {
        InitialVersion = 0,
        SerializeStructSize,
        // -----<new versions can be added above this line>-----
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    };
}

public struct FPropertyBagPropertyDesc
{
    public FPackageIndex ValueTypeObject;
    public FGuid ID;
    public FName Name;
    [JsonConverter(typeof(StringEnumConverter))]
    public EPropertyBagPropertyType ValueType;
    [JsonConverter(typeof(StringEnumConverter))]
    public EPropertyBagContainerType ContainerType;
    public FPropertyBagContainerTypes ContainerTypes;
    public FPropertyBagPropertyDescMetaData[] MetaData;
    public FPackageIndex MetaClass;

    public FPropertyBagPropertyDesc(FAssetArchive Ar)
    {
        ValueTypeObject = new FPackageIndex(Ar);
        ID = Ar.Read<FGuid>();
        Name = Ar.ReadFName();
        ValueType = Ar.Read<EPropertyBagPropertyType>();

        if (FPropertyBagCustomVersion.Get(Ar) < FPropertyBagCustomVersion.Type.ContainerTypes)
        {
            ContainerType = Ar.Read<EPropertyBagContainerType>();
            if (ContainerType != EPropertyBagContainerType.None)
                ContainerTypes = new FPropertyBagContainerTypes([ContainerType]);
        }
        else
        {
            ContainerTypes = new FPropertyBagContainerTypes(Ar);
        }

        var bHasMetaData = Ar.ReadBoolean();
        if (bHasMetaData)
        {
            MetaData = Ar.ReadArray(() => new FPropertyBagPropertyDescMetaData(Ar));
            if (FPropertyBagCustomVersion.Get(Ar) >= FPropertyBagCustomVersion.Type.MetaClass)
            {
                MetaClass = new FPackageIndex(Ar);
            }
        }
    }
}

public struct FPropertyBagPropertyDescMetaData(FAssetArchive Ar)
{
    public FName Key = Ar.ReadFName();
    public string Value = Ar.ReadFString();
}

public struct FPropertyBagContainerTypes
{
    public EPropertyBagContainerType[] Types;

    public FPropertyBagContainerTypes(EPropertyBagContainerType[] types)
    {
        Types = types;
    }

    public FPropertyBagContainerTypes(FAssetArchive Ar)
    {
        Types = Ar.ReadArray(Ar.Read<byte>(), Ar.Read<EPropertyBagContainerType>);
    }
}
