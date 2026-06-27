using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public readonly struct FValueType : IUStruct
    {
        public readonly EManagedArrayType ArrayType;
        public readonly FName GroupIndexDependency;
        public readonly bool Saved;
        public readonly FManagedArrayBase? Value;

        public FValueType(FChaosArchive Ar, int version)
        {
            if (Ar.Game >= EGame.GAME_UE5_0)
                version = Ar.Read<int>(); // 4
            Debug.Assert(version == 4);
            var arrayTypeAsInt = Ar.Read<int>();
            ArrayType = (EManagedArrayType) arrayTypeAsInt;

            if (Ar.Game == EGame.GAME_MarvelRivals)
            {
                ArrayType = ArrayType switch
                {
                    // Map 42 => 44
                    EManagedArrayType.FFImplicitObjectRefCountedPtrType => EManagedArrayType.Transform3fType,
                    _ => ArrayType
                };
            }

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

            Value = FManagedArrayBase.NewManagedTypedArray(ArrayType);

            bool bNewSavedBehavior = FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.ManagedArrayCollectionAlwaysSerializeValue;
            if (bNewSavedBehavior || Saved)
            {
                Value.Serialize(Ar);
            }
        }
    }
}
