using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine
{
    public abstract class TPerPlatformProperty : IUStruct
    {
        public abstract object Value { get; }

        public class FPerPlatformBool : TPerPlatformProperty
        {
            public readonly bool bCooked;
            public readonly bool Default;
            public override object Value => Default;

            public FPerPlatformBool()
            {
                
            }
            
            public FPerPlatformBool(FAssetArchive Ar)
            {
                bCooked = Ar.ReadBoolean();
                Default = Ar.ReadBoolean();
            }
        }

        public class FPerPlatformFloat : TPerPlatformProperty
        {
            public readonly bool bCooked;
            public readonly float Default;
            public override object Value => Default;

            public FPerPlatformFloat()
            {
                
            }

            public FPerPlatformFloat(FAssetArchive Ar)
            {
                bCooked = Ar.ReadBoolean();
                Default = Ar.Read<float>();
            }
        }

        public class FPerPlatformInt : TPerPlatformProperty
        {
            public readonly bool bCooked;
            public readonly int Default;
            public override object Value => Default;

            public FPerPlatformInt()
            {
                
            }

            public FPerPlatformInt(FAssetArchive Ar)
            {
                bCooked = Ar.ReadBoolean();
                Default = Ar.Read<int>();
            }
        }
    }
}
