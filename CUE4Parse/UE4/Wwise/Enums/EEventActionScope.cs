namespace CUE4Parse.UE4.Wwise.Enums;

public enum EEventActionScope : byte
{
    None = 0x0,
    GameObject,
    Global,
    GameObjectId,
    All,
    GlobalGameObject,
    AllExceptId = 0x09,
    Ducking = 0x20
}
