using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class UClass : UStruct
    {
        /** Used to check if the class was cooked or not */
        public bool bCooked;

        /** Class flags; See EClassFlags for more information */
        public uint ClassFlags; // EClassFlags

        /** The required type for the outer of instances of this class */
        public FPackageIndex ClassWithin;

        /** This is the blueprint that caused the generation of this class, or null if it is a native compiled-in class */
        public FPackageIndex ClassGeneratedBy;

        /** Which Name.ini file to load Config variables out of */
        public FName ClassConfigName;

        /** The class default object; used for delta serialization and object initialization */
        public FPackageIndex ClassDefaultObject;

        /** Map of all functions by name contained in this class */
        public Dictionary<FName, FPackageIndex /*UFunction*/> FuncMap;

        /**
         * The list of interfaces which this class implements, along with the pointer property that is located at the offset of the interface's vtable.
         * If the interface class isn't native, the property will be null.
         */
        public FImplementedInterface[] Interfaces;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            // serialize the function map
            FuncMap = new Dictionary<FName, FPackageIndex>();
            var funcMapNum = Ar.Read<int>();
            for (var i = 0; i < funcMapNum; i++)
            {
                FuncMap[Ar.ReadFName()] = new FPackageIndex(Ar);
            }

            // Class flags first.
            ClassFlags = Ar.Read<uint>();

            // Variables.
            ClassWithin = new FPackageIndex(Ar);
            ClassConfigName = Ar.ReadFName();

            ClassGeneratedBy = new FPackageIndex(Ar);

            // Load serialized interface classes
            Interfaces = Ar.ReadArray(() => new FImplementedInterface(Ar));

            var bDeprecatedScriptOrder = Ar.ReadBoolean();
            var dummy = Ar.ReadFName();

            if ((int) Ar.Ver >= 241 /*VER_UE4_ADD_COOKED_TO_UCLASS*/)
            {
                bCooked = Ar.ReadBoolean();
            }

            // Defaults.
            ClassDefaultObject = new FPackageIndex(Ar);
        }

        public class FImplementedInterface
        {
            /** the interface class */
            public FPackageIndex Class;

            /** the pointer offset of the interface's vtable */
            public int PointerOffset;

            /** whether or not this interface has been implemented via K2 */
            public bool bImplementedByK2;

            public FImplementedInterface(FAssetArchive Ar)
            {
                Class = new FPackageIndex(Ar);
                PointerOffset = Ar.Read<int>();
                bImplementedByK2 = Ar.ReadBoolean();
            }
        }
    }
}