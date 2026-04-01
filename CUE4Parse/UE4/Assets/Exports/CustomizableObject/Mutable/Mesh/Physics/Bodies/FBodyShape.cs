using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;

public class FBodyShape
{
    [JsonIgnore] public int Version = 1; 
    public string Name;
    public uint Flags;

    public FBodyShape(FMutableArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_6) Version = Ar.Read<int>();
        if (Version >= 1)
            Name = Ar.ReadFString();
        else
            Name = Ar.ReadString();
        Flags = Ar.Read<uint>();
    }
}
