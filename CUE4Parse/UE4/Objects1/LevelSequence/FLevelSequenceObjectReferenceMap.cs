using CUE4Parse.UE4.Objects.Core.Misc;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.LevelSequence
{
    public readonly struct FLevelSequenceObjectReferenceMap : IUStruct, IReadOnlyDictionary<FGuid, FLevelSequenceLegacyObjectReference>
    {
        public readonly IDictionary<FGuid, FLevelSequenceLegacyObjectReference> Map;

        public FLevelSequenceObjectReferenceMap(FArchive Ar)
        {
            Map = new Dictionary<FGuid, FLevelSequenceLegacyObjectReference>(Ar.Read<int>());
            for (int i = 0; i < Map.Count; i++)
            {
                Map[Ar.Read<FGuid>()] = new FLevelSequenceLegacyObjectReference(Ar);
            }
        }

        public FLevelSequenceLegacyObjectReference this[FGuid key] => Map[key];
        public IEnumerable<FGuid> Keys => Map.Keys.AsEnumerable();
        public IEnumerable<FLevelSequenceLegacyObjectReference> Values => Map.Values.AsEnumerable();
        public int Count => Map.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(FGuid key) => Map.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<FGuid, FLevelSequenceLegacyObjectReference>> GetEnumerator() => Map.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(FGuid key, out FLevelSequenceLegacyObjectReference value) => Map.TryGetValue(key, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Map).GetEnumerator();
    }
}