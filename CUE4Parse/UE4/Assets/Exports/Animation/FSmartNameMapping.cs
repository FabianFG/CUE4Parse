using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
	[JsonConverter(typeof(FSmartNameMappingConverter))]
    public class FSmartNameMapping
    {
		public readonly Dictionary<FName, FGuid> GuidMap;
		public readonly Dictionary<ushort, FName> UidMap;
		public readonly Dictionary<FName, FCurveMetaData> CurveMetaDataMap;

		public FSmartNameMapping(FArchive Ar)
        {
            var frwObjVer = FFrameworkObjectVersion.Get(Ar);
            var frwAniVer = FAnimPhysObjectVersion.Get(Ar);
			if (frwObjVer >= FFrameworkObjectVersion.Type.SmartNameRefactor)
			{
				if (frwAniVer < FAnimPhysObjectVersion.Type.SmartNameRefactorForDeterministicCooking)
				{
					var mapLength = Ar.Read<int>();
					GuidMap = new Dictionary<FName, FGuid>(mapLength);
					for (var i = 0; i < mapLength; i++)
                    {
						GuidMap[Ar.ReadFName()] = Ar.Read<FGuid>();
                    }
				}
			}
			else if (Ar.Ver >= EUnrealEngineObjectUE4Version.SKELETON_ADD_SMARTNAMES)
			{
				Ar.Read<ushort>();
				var mapLength = Ar.Read<int>();
				UidMap = new Dictionary<ushort, FName>(mapLength);
				for (int i = 0; i < mapLength; i++)
				{
					UidMap[Ar.Read<ushort>()] = Ar.ReadFName();
				}
			}

			if (frwObjVer >= FFrameworkObjectVersion.Type.MoveCurveTypesToSkeleton)
			{
				var mapLength = Ar.Read<int>();
				CurveMetaDataMap = new Dictionary<FName, FCurveMetaData>(mapLength);
				for (var i = 0; i < mapLength; i++)
				{
					CurveMetaDataMap[Ar.ReadFName()] = new FCurveMetaData(Ar, frwAniVer);
				}
			}
		}
    }
}
