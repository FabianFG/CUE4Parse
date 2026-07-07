using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.GameTypes.LegoBatman.Assets;

public class UWubDialogueEvent : UObject
{
    public FWubStruct2 Sequence;
    public HashSet<FName> Wems = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Sequence = new FWubStruct2(Ar, Wems);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(Sequence));
        serializer.Serialize(writer, Sequence);
    }

    public static FWubStructBase? ReadStruct(FAssetArchive Ar, HashSet<FName> wems)
    {
        var type = Ar.Read<byte>();
        return type switch
        {
            0 => null,
            1 => new FWubStruct1(Ar, wems),
            2 => new FWubStruct2(Ar, wems),
            3 => new FWubStruct3(Ar, wems),
            4 => new FWubStruct4(Ar, wems),
            _ => throw new ParserException("Unknown FWubStruct type")
        };
    }

    public class FWubStructBase
    {
        public FInstancedStruct[] DialogueTriggerConditions;
        public FWubStructBase?[] InnerSequence;

        public FWubStructBase(FAssetArchive Ar)
        {
            Ar.Position += 1;
            DialogueTriggerConditions = Ar.ReadArray(() => new FInstancedStruct(Ar));
            Ar.Position += 1;
        }
    }

    public class FWubStruct1 : FWubStructBase
    {
        public FName Character;
        public FName WemName;
        public FPackageIndex AudioEvent;
        public FName AudioTag;

        public FWubStruct1(FAssetArchive Ar, HashSet<FName> wems) : base(Ar)
        {
            Character = Ar.ReadFName();
            WemName = Ar.ReadFName();
            wems.Add(WemName);
            Ar.Position += 4;
            Ar.SkipFixedArray(10); // platforms
            AudioEvent = new FPackageIndex(Ar);
            Ar.Position += 18;
            AudioTag = Ar.ReadFName();
        }
    }

    public class FWubStruct2 : FWubStructBase
    {
        public FName EventId;
        public float DelayInSeconds;
        public float TriggerChance;
        public FName AssociatedActor;

        public FWubStruct2(FAssetArchive Ar, HashSet<FName> wems) : base(Ar)
        {
            EventId = Ar.ReadFName();
            Ar.Position += 2;
            DelayInSeconds = Ar.Read<float>();
            TriggerChance = Ar.Read<float>();
            AssociatedActor = Ar.ReadFName();
            var count = Ar.Read<int>();
            Log.Information("FStruct2 {0} {1} {2} {3}, inner {4}", EventId, AssociatedActor, DelayInSeconds, TriggerChance, count);
            InnerSequence = Ar.ReadArray(count, () => ReadStruct(Ar, wems));
        }
    }

    public class FWubStruct3 : FWubStructBase
    {
        public FWubStructBase? AdditionalSequence;

        public FWubStruct3(FAssetArchive Ar, HashSet<FName> wems) : base(Ar)
        {
            var data = Ar.ReadBytes(3);
            if (data[0] == 2)
            {
                AdditionalSequence = ReadStruct(Ar, wems);
            }
            else
            {
                if (Ar.Read<byte>() != 0)
                {
                    Ar.Position -= 1;
                    AdditionalSequence = ReadStruct(Ar, wems);
                }
            }
            InnerSequence = Ar.ReadArray(() => ReadStruct(Ar, wems));
        }
    }

    public class FWubStruct4 : FWubStructBase
    {
        public FWubStruct4(FAssetArchive Ar, HashSet<FName> wems) : base(Ar)
        {
            Ar.Position += 1;
            var count = Ar.Read<int>();
            InnerSequence = new FWubStructBase[count];
            for (var i = 0; i < count; i++)
            {
                Ar.SkipFixedArray(12);
                InnerSequence[i] = ReadStruct(Ar, wems);
            }
        }
    }
}

