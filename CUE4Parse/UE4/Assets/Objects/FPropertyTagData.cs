using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public abstract class FPropertyTagData
    {
        public override string ToString() => GetType().Name;

        public class StructProperty : FPropertyTagData
        {
            public FName StructName;
            public FGuid StructGuid;

            public StructProperty(FAssetArchive Ar)
            {
                StructName = Ar.ReadFName();
                StructGuid = Ar.Read<FGuid>();
            }
        }

        public class ByteProperty : FPropertyTagData
        {
            public FName ByteName;

            public ByteProperty(FAssetArchive Ar)
            {
                ByteName = Ar.ReadFName();
            }
        }
        
        public class EnumProperty : FPropertyTagData
        {
            public FName EnumName;

            public EnumProperty(FAssetArchive Ar)
            {
                EnumName = Ar.ReadFName();
            }
        }

        public class ArrayProperty : FPropertyTagData
        {
            public FName InnerType;

            public ArrayProperty(FAssetArchive Ar)
            {
                InnerType = Ar.ReadFName();
            }
        }
        
        public class SetProperty : FPropertyTagData
        {
            public FName InnerType;

            public SetProperty(FAssetArchive Ar)
            {
                InnerType = Ar.ReadFName();
            }
        }

        public class MapProperty : FPropertyTagData
        {
            public FName InnerType;
            public FName ValueType;

            public MapProperty(FAssetArchive Ar)
            {
                InnerType = Ar.ReadFName();
                ValueType = Ar.ReadFName();
            }
        }

        public class BoolProperty : FPropertyTagData
        {
            public bool BoolVal;

            public BoolProperty(FArchive Ar)
            {
                BoolVal = Ar.ReadFlag();
            }
        }
    }
}
