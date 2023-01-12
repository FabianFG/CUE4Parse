using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.Rig
{
    public class UDNAAsset : UObject
    {
        public string DNAFileName { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            DNAFileName = GetOrDefault<string>(nameof(DNAFileName));

            if (FDNAAssetCustomVersion.Get(Ar) >= FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
            {
                // var magic = Ar.Read<uint>();
                // if (magic != 4279876)
                //     throw new Exception("invalid dna magic");
                //
                // var GetArchetype = Ar.Read<EArchetype>();
                // var GetGender = Ar.Read<EGender>();
                // var GetAge = Ar.Read<ushort>();
                // var GetMetaDataCount = Ar.Read<uint>();
                // for (int i = 0; i < GetMetaDataCount; i++)
                // {
                //     var key = Ar.ReadFString();
                //     var value = Ar.ReadFString();
                // }
                // Behavior
                // Geometry
            }
        }
    }
}
