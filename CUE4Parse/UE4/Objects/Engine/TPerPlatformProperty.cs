using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine;

public abstract class TPerPlatformProperty : IUStruct
{
    public abstract object Value { get; }

    public class FPerPlatformBool : TPerPlatformProperty
    {
        public readonly bool bCooked;
        public readonly bool Default;
        public readonly Dictionary<FName, bool> PerPlatform;
        public override object Value => Default;

        public FPerPlatformBool() { }

        public FPerPlatformBool(FAssetArchive Ar)
        {
            bCooked = Ar.ReadBoolean();
            Default = Ar.ReadBoolean();
            if (Ar.IsFilterEditorOnly && !bCooked)
            {
                PerPlatform = Ar.ReadMap(Ar.ReadFName, Ar.Read<bool>);
            }
        }
    }

    public class FPerPlatformFloat : TPerPlatformProperty
    {
        public readonly bool bCooked;
        public readonly float Default;
        public readonly Dictionary<FName, float> PerPlatform;
        public override object Value => Default;

        public FPerPlatformFloat() { }
        public FPerPlatformFloat(float value) { Default = value; }

        public FPerPlatformFloat(FAssetArchive Ar)
        {
            bCooked = Ar.ReadBoolean();
            Default = Ar.Read<float>();
            if (Ar.IsFilterEditorOnly && !bCooked)
            {
                PerPlatform = Ar.ReadMap(Ar.ReadFName, Ar.Read<float>);
            }
        }
    }

    public class FPerPlatformInt : TPerPlatformProperty
    {
        public readonly bool bCooked;
        public readonly int Default;
        public readonly Dictionary<FName, int> PerPlatform;
        public override object Value => Default;

        public FPerPlatformInt() { }

        public FPerPlatformInt(FAssetArchive Ar)
        {
            bCooked = Ar.ReadBoolean();
            Default = Ar.Read<int>();
            if (Ar.IsFilterEditorOnly && !bCooked)
            {
                PerPlatform = Ar.ReadMap(Ar.ReadFName, Ar.Read<int>);
            }
        }
    }

    public class FPerPlatformFrameRate : TPerPlatformProperty
    {
        public readonly bool bCooked;
        public readonly FFrameRate Default;
        public override object Value => Default;

        public FPerPlatformFrameRate() { }

        public FPerPlatformFrameRate(FArchive Ar)
        {
            bCooked = Ar.ReadBoolean();
            Default = Ar.Read<FFrameRate>();
        }
    }

    public class FPerPlatformFString : TPerPlatformProperty
    {
        public readonly bool bCooked;
        public readonly string Default;
        public override object Value => Default;

        public FPerPlatformFString() { }

        public FPerPlatformFString(FArchive Ar)
        {
            bCooked = Ar.ReadBoolean();
            Default = Ar.ReadFString();
        }
    }
}
