using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Shaders
{
    // https://github.com/EpicGames/UnrealEngine/blob/803688920e030c9a86c3659ac986030fba963833/Engine/Source/Runtime/RenderCore/Public/ShaderCodeArchive.h#L134
    public class FShaderTypeHashes
    {
        public ulong[] Data;

        public FShaderTypeHashes(FArchive Ar)
        {
            Data = Ar.ReadArray<ulong>();
        }
    }
}
