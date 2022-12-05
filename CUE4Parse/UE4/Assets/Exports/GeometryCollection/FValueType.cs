using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public readonly struct FValueType : IUStruct
    {
        public readonly EManagedArrayType ArrayType;
        public readonly FName GroupIndexDependency;
        public readonly bool Saved;
        // public readonly FManagedArrayBase? Value;

        public FValueType(FAssetArchive Ar, int version)
        {
            var arrayTypeAsInt = Ar.Read<int>();
            ArrayType = (EManagedArrayType) arrayTypeAsInt;

            if (version < 4)
            {
                var arrayScopeAsInt = Ar.Read<int>();
            }

            if (version >= 2)
            {
                GroupIndexDependency = Ar.ReadFName();
                Saved = Ar.ReadBoolean();
            }
            else
            {
                GroupIndexDependency = new FName();
                Saved = false;
            }

            // if (Value == null)
            // {
            //     Value = NewManagedTypedArray(ValueIn.ArrayType);
            // }
            //
            // bool bNewSavedBehavior = FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.ManagedArrayCollectionAlwaysSerializeValue;
            // if (bNewSavedBehavior || Saved)
            // {
            //     Value = new FManagedArrayBase();
            // }
        }
    }
}
