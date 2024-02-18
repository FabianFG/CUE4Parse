namespace CUE4Parse.UE4.Wwise.Enums
{
    public enum EEventActionScope : byte
    {
        GameObject = 0x01,
        Global,
        GameObjectId,
        GameObjectState,
        All,
        AllExceptId = 0x09,
    }
}
