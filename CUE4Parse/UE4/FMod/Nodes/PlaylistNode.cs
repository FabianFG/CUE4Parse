using System.IO;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class PlaylistNode
{
    public readonly EPlaylistPlayMode PlayMode;
    public readonly EPlaylistSelectionMode SelectionMode;
    public readonly FPlaylistEntry[] Entries;

    public PlaylistNode(BinaryReader Ar)
    {
        PlayMode = (EPlaylistPlayMode) Ar.ReadInt32();
        SelectionMode = (EPlaylistSelectionMode) Ar.ReadInt32();
        Entries = FModReader.ReadElemListImp<FPlaylistEntry>(Ar);
    }
}
