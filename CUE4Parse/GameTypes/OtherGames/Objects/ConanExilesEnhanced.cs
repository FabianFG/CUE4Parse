
public class UUniqueID : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game is EGame.GAME_ConanExilesEnhanced) CustomGameData = Ar.Read<long>();
        else base.Deserialize(Ar, validPos);
    }
}

public class UBuildingSocketComponent : UInstancedStaticMeshComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        var memorySize = Ar.Read<int>();
        // SocketStaticData
        CustomGameData = Ar.ReadArray(() => new FSocketStaticData(Ar));
    }

    public class FSocketStaticData(FAssetArchive Ar)
    {
        public EBuildingSocketType[] SocketTypes = Ar.ReadBoolean() ? Ar.ReadArray<EBuildingSocketType>() : [];
        public EBuildingSocketType[] TargetSocketTypes = Ar.ReadBoolean() ? Ar.ReadArray<EBuildingSocketType>() : [];
        public int AttachToCost = Ar.Read<int>();
        public int AttachCost = Ar.Read<int>();
        public ESocketConfiguration[] OverrideSocketRotations = Ar.ReadBoolean() ? Ar.ReadArray<ESocketConfiguration>() : [];
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EBuildingSocketType : byte
    {
        Building = 0,
        Door = 1,
        Pillar = 2,
        WallTrim = 3,
        CastleWall = 4,
        Fence_Foundation = 5,
        Fence_Wall = 6,
        Siege_Foundation = 7,
        Gate = 8,
        Rampart_Defense = 9,
        Corner = 10,
        Hatch = 11,
        Strut = 12,
        Ladder = 13,
        Window = 14,
        Custom_Socket_00 = 15,
        Custom_Socket_01 = 16,
        Custom_Socket_02 = 17,
        Custom_Socket_03 = 18,
        Custom_Socket_04 = 19,
        Custom_Socket_05 = 20,
        Custom_Socket_06 = 21,
        Custom_Socket_07 = 22,
        Custom_Socket_08 = 23,
        Custom_Socket_09 = 24,
        Chimney = 25,
        Chimney_Wall = 26,
        Shutters = 27,
        DoubleDoor = 28,
        Scaffolding = 29,
        CurvedWall = 30,
        SlidingDoor = 31,
        Hearth = 32,
        Rope_Bridge = 33
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ESocketConfiguration : byte
    {
        Normal = 0,
        Rotated180 = 1,
        Rotated90 = 2,
        Rotated270 = 3,
        Rotated120 = 4,
        Rotated240 = 5,
    }
}
