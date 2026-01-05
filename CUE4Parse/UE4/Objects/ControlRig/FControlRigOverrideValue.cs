using System;
using System.Linq;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse.UE4.Objects.ControlRig;

public class FControlRigOverrideValue
{
    public FName SubjectKey; // TOptional
    public FPropertyInfo[] Properties;
    public long OffsetForData;

    public FControlRigOverrideValue(FAssetArchive Ar)
    {
        if (Ar.ReadBoolean()) SubjectKey = Ar.ReadFName();
        Properties = Ar.ReadArray(() => new FPropertyInfo(Ar));
        var saved = Ar.Position;
        OffsetForData = Ar.Read<long>();
        try
        {
            if (OffsetForData == 0) return;

            var property = Properties.Last();
            property.Size = Ar.Read<int>();
            property.Hash = Ar.Read<uint>();
            if (property.Property.ResolvedOwner!.TryLoad<UStruct>(out var struc))
            {
                var type = struc.Name;
                Struct? propMappings = null;
                if (struc is UScriptClass)
                    Ar.Owner!.Mappings?.Types.TryGetValue(type, out propMappings);
                else
                    propMappings = new SerializedStruct(Ar.Owner!.Mappings, struc);

                var propInfo = propMappings?.Properties.FirstOrDefault(x => x.Value.Name.Equals(property.Property.Path[0].Text));
                
                if (propInfo != null)
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
