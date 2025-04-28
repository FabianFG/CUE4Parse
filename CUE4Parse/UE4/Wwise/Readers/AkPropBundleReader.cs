using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Readers
{
    public struct AkProp
    {
        public byte ID;
        public float Value;
    }

    public struct AkPropRange
    {
        public byte ID;
        public float Min;
        public float Max;
    }

    public static class AkPropBundleReader
    {
        public static List<AkProp> ReadProps(this FArchive Ar)
        {
            int count = Ar.Read<byte>();
            var props = new List<AkProp>(count);
            for (int i = 0; i < count; i++)
            {
                byte id = Ar.Read<byte>();
                float value = Ar.Read<float>();
                props.Add(new AkProp { ID = id, Value = value });
            }
            return props;
        }

        public static List<AkPropRange> ReadPropRanges(this FArchive Ar)
        {
            int count = Ar.Read<byte>();
            var ranges = new List<AkPropRange>(count);
            for (int i = 0; i < count; i++)
            {
                byte id = Ar.Read<byte>();
                float min = Ar.Read<float>();
                float max = Ar.Read<float>();
                ranges.Add(new AkPropRange { ID = id, Min = min, Max = max });
            }
            return ranges;
        }
    }
}
