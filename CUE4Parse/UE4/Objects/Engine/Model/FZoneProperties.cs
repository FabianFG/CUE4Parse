using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Model
{
    public readonly struct FZoneProperties : IUStruct
    {
        public readonly FPackageIndex ZoneActor;
        public readonly float LastRenderTime;
        public readonly FZoneSet Connectivity;
        public readonly FZoneSet Visibility;
    }

    public readonly struct FZoneSet : IUStruct
    {
        public readonly ulong MaskBits;
    }
}