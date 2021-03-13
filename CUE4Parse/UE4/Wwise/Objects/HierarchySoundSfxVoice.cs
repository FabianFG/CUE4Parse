using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchySoundSfxVoice : AbstractHierarchy
    {
        public readonly ESoundConversion SoundConversion;
        public readonly ESoundSource SoundSource;
        public readonly uint SoundId;
        public readonly uint SourceId;
        public readonly uint? WemOffset;
        public readonly uint? WemLength;
        public readonly ESoundType SoundType;
        
        public HierarchySoundSfxVoice(FArchive Ar) : base(Ar)
        {
            Ar.Position += 2;
            SoundConversion = Ar.Read<ESoundConversion>();
            Ar.Position += 1;
            SoundSource = Ar.Read<ESoundSource>();
            SoundId = Ar.Read<uint>();
            SourceId = Ar.Read<uint>();
            
            if (SoundSource == ESoundSource.Embedded)
            {
                WemOffset = Ar.Read<uint>();
                WemLength = Ar.Read<uint>();
            }
            
            SoundType = Ar.Read<ESoundType>();
            
            //TODO
        }
    }
}