using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Niagara
{
    public class UNiagaraSystem : Assets.Exports.UObject
    {
        public List<FStructFallback> NiagaraEmitterCompiledDataStructs;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (FNiagaraCustomVersion.Get(Ar) >= FNiagaraCustomVersion.Type.ChangeEmitterCompiledDataToSharedRefs)
            {
                var emitterCompiledDataNum = Ar.Read<int>();

                NiagaraEmitterCompiledDataStructs = new List<FStructFallback>();
                for (var emitterIndex = 0; emitterIndex < emitterCompiledDataNum; ++emitterIndex)
                {
                    NiagaraEmitterCompiledDataStructs.Add(new FStructFallback(Ar, "NiagaraEmitterCompiledData"));
                }
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (NiagaraEmitterCompiledDataStructs is { Count: > 0 })
            {
                writer.WritePropertyName("EmitterCompiledStructs");
                serializer.Serialize(writer, NiagaraEmitterCompiledDataStructs);
            }
        }
    }
}
