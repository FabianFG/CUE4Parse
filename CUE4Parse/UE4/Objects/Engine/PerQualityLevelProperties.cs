using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class FPerQualityLevelInt : IUStruct
    {
        public readonly bool bCooked;
        public readonly int Default;
        public readonly Dictionary<int, int> PerQuality;

        public FPerQualityLevelInt(FArchive Ar)
        {
            bCooked = Ar.ReadBoolean();
            Default = Ar.Read<int>();
            PerQuality = new Dictionary<int, int>();
            int perQualityNum = Ar.Read<int>();
            for (int i = 0; i < perQualityNum; i++)
            {
                PerQuality[Ar.Read<int>()] = Ar.Read<int>();
            }
        }

        public FPerQualityLevelInt()
        {
            PerQuality = new Dictionary<int, int>();
        }
    }

    public class FPerQualityLevelFloat : IUStruct
    {
        public readonly bool bCooked;
        public readonly float Default;
        public readonly Dictionary<int, float> PerQuality;

        public FPerQualityLevelFloat(FArchive Ar)
        {
            bCooked = Ar.ReadBoolean();
            Default = Ar.Read<float>();
            PerQuality = new Dictionary<int, float>();
            int perQualityNum = Ar.Read<int>();
            for (int i = 0; i < perQualityNum; i++)
            {
                PerQuality[Ar.Read<int>()] = Ar.Read<float>();
            }
        }

        public FPerQualityLevelFloat()
        {
            PerQuality = new Dictionary<int, float>();
        }
    }
}