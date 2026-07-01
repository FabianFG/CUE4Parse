using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Objects.ControlRig;

public class FControlRigOverrideValue
{
    public FName SubjectKey; // TOptional
    public FPropertyInfo[] Properties;
    public long OffsetForData;
    public FSoftObjectPath OwnerStructPath;
    public string? TempPath;

    public FControlRigOverrideValue(FAssetArchive Ar)
    {
        if (Ar.ReadBoolean()) SubjectKey = Ar.ReadFName();
        bool bStoresOnlyPathAndLeafProperty = FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.OverridesStorePathAndLeafPropertyOnly;
        if (bStoresOnlyPathAndLeafProperty)
        {
            OwnerStructPath = new FSoftObjectPath(Ar);
            TempPath = Ar.ReadBoolean() ? Ar.ReadFString() : null;
        }
        else
        {
            Properties = Ar.ReadArray(() => new FPropertyInfo(Ar));
        }
        var saved = Ar.Position;
        OffsetForData = Ar.Read<long>();
        try
        {
            if (OffsetForData == 0) return;
            var property = Properties?.Last();
            if (property is null)
            {
                if (!bStoresOnlyPathAndLeafProperty) return;
                else
                {
                    property = new FPropertyInfo() { ArrayIndex =  -1 };
                    if (TempPath != null)
                        property.Property = new FFieldPath() { Path = TempPath.Split("->").Select(x => new FName(x)).ToArray()};
                    else
                        return;
                    Properties = [property];
                }
            }
            if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.OverridesStoreLeafPropertyHashOnly)
            {
                property.Hash = Ar.Read<uint>();
            }
            else if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.OverridesStoreTOCDataForProperties)
            {
                property.Size = Ar.Read<int>();
                property.Hash = Ar.Read<uint>();
            }

            if ((!bStoresOnlyPathAndLeafProperty && property.Property.ResolvedOwner!.TryLoad<UStruct>(out var struc))
                || (bStoresOnlyPathAndLeafProperty && OwnerStructPath.TryLoad<UStruct>(out struc)))
            {
                var type = struc.Name;
                Struct? propMappings = null;
                if (struc is UScriptClass)
                    Ar.Owner!.Mappings?.Types.TryGetValue(type, out propMappings);
                else
                    propMappings = new SerializedStruct(Ar.Owner!.Mappings, struc);

                KeyValuePair<int, PropertyInfo>? propInfo = new();
                for (int index = 0; index < property.Property.Path.Length; index++)
                {
                    FName name = property.Property.Path[index];
                    propInfo = propMappings?.Properties.FirstOrDefault(x =>
                        x.Value.Name.Equals(name.Text, StringComparison.OrdinalIgnoreCase));
                    if (propInfo?.Value is null) return;
                    if (index < property.Property.Path.Length - 1 && propInfo.Value.Value.MappingType.Type is "StructProperty")
                    {
                        var inner = propInfo.Value.Value.MappingType;
                        if (inner.StructType is not null)
                            Ar.Owner!.Mappings?.Types.TryGetValue(inner.StructType, out propMappings);
                        else if (inner.Struct is not null)
                            propMappings = new SerializedStruct(Ar.Owner!.Mappings, inner.Struct);
                        else
                            return;
                    }
                }

                if (propInfo?.Value is not null)
                {
                    var propType = propInfo.Value.Value;
                    var arrayIndex = property.ArrayIndex;
                    if (arrayIndex != -1 && propType.MappingType.Type is "ArrayProperty")
                    {
                        var tagData = new FPropertyTagData(propType.MappingType);
                        var tag = FPropertyTagType.ReadPropertyTagType(Ar, propType.MappingType.InnerType?.Type, tagData.InnerTypeData, ReadType.NORMAL);
                        property.Value = new FPropertyTag(propType.MappingType.Type, tag, tagData);
                    }
                    else
                    {
                        property.Value = new FPropertyTag(Ar, propInfo.Value.Value, ReadType.NORMAL);
                    }
                }
                else
                {
                    Log.Warning("Failed to find property {Property} in struct {Struct} via mappings", property.Property, struc.Name);
                }
            }

        }
        catch (Exception e)
        {
            Log.Error(e, "Error reading FControlRigOverrideValue properties");
        }
        finally
        {
            Ar.Position = saved + OffsetForData;
        }
    }
}
