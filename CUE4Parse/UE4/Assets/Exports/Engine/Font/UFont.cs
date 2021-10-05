using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font
{
    public class UFont : UObject
    {
        public Dictionary<ushort, ushort>? CharRemap;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var num = Ar.Read<int>();
            CharRemap = new Dictionary<ushort, ushort>(num);
            for (var i = 0; i < num; ++i)
            {
                CharRemap[Ar.Read<ushort>()] = Ar.Read<ushort>();
            }
        }
    }
}
