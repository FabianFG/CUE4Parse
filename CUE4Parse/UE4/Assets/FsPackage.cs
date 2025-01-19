using System;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets;

public class FObject
{
    public FName Name;
    public FName Type;
    public Lazy<FObject> Outer;

    public override string ToString() => $"{Name} ({Type})";
}

public class FsPackage
{
    public readonly FName Name;
    public readonly FNameEntrySerialized[] NameMap;
    public readonly FObject[] ExportMap;

    public FsPackage(FArchive Ar, IoGlobalData GlobalData)
    {
        FName FromMappedName(FMappedName mappedName)
        {
            return new FName(mappedName, mappedName.IsGlobal ? GlobalData.GlobalNameMap : NameMap);
        }

        FMappedName mappedName;
        int exportMapOffset;
        int exportCount;

        if (Ar.Game >= EGame.GAME_UE5_0)
        {
            var bHasVersioningInfo = Ar.Read<uint>();
            Ar.Position += 4;
            mappedName = Ar.Read<FMappedName>();
            Ar.Position += sizeof(EPackageFlags) + 12;
            exportMapOffset = Ar.Read<int>();
            exportCount = (Ar.Read<int>() - exportMapOffset) / FExportMapEntry.Size;
            Ar.Position += 4 * (Ar.Game >= EGame.GAME_UE5_3 ? 3 : 1);

            if (bHasVersioningInfo != 0)
            {
                Ar.Position += sizeof(EZenPackageVersion) + 12 + sizeof(int);
            }

            NameMap = FNameEntrySerialized.LoadNameBatch(Ar);
        }
        else
        {
            mappedName = Ar.Read<FMappedName>();
            Ar.Position += sizeof(EPackageFlags) + 12;
            var nameMapNamesOffset = Ar.Read<int>();
            Ar.Position += 8;
            var nameMapHashesSize = Ar.Read<int>();
            Ar.Position += 4;
            exportMapOffset = Ar.Read<int>();
            exportCount = (Ar.Read<int>() - exportMapOffset) / FExportMapEntry.Size;

            Ar.Position = nameMapNamesOffset;
            NameMap = FNameEntrySerialized.LoadNameBatch(Ar, nameMapHashesSize / sizeof(ulong) - 1);
        }

        Name = FromMappedName(mappedName);

        Ar.Position = exportMapOffset;
        ExportMap = Ar.ReadArray(exportCount, () =>
        {
            var start = Ar.Position;
            var obj = new FObject();

            Ar.Position += 16;
            obj.Name = FromMappedName(Ar.Read<FMappedName>());
            Ar.Position += 8;

            var classIndex = Ar.Read<FPackageObjectIndex>();
            if (classIndex.IsScriptImport)
            {
                if (GlobalData.ScriptObjectEntriesMap.TryGetValue(classIndex, out var objectEntry))
                {
                    obj.Type = FromMappedName(objectEntry.ObjectName);
                }
            }

            Ar.Position = start + FExportMapEntry.Size;
            return obj;
        });
    }

    public FsPackage(FArchive Ar)
    {
        Ar.Position += 4;

        var fileVersionUE = new FPackageFileVersion();
        var legacyFileVersion = Ar.Read<int>();

        if (legacyFileVersion < 0)
        {
            if (legacyFileVersion != -4)
            {
                Ar.Position += 4;
            }
            fileVersionUE.FileVersionUE4 = Ar.Read<int>();
            if (legacyFileVersion <= -8)
            {
                fileVersionUE.FileVersionUE5 = Ar.Read<int>();
            }
            Ar.Position += sizeof(EUnrealEngineObjectLicenseeUEVersion);

            switch (FCustomVersionContainer.DetermineSerializationFormat(legacyFileVersion))
            {
                case ECustomVersionSerializationFormat.Enums:
                {
                    var length = Ar.Read<int>();
                    Ar.Position += 8 * length;
                    break;
                }
                case ECustomVersionSerializationFormat.Guids:
                {
                    var length = Ar.Read<int>();
                    for (var i = 0; i < length; i++)
                    {
                        Ar.Position += 20;
                        var friendlyNameLength = Ar.Read<int>();
                        Ar.Position += friendlyNameLength;
                    }
                    break;
                }
                case ECustomVersionSerializationFormat.Optimized:
                {
                    var length = Ar.Read<int>();
                    Ar.Position += 20 * length;
                    break;
                }
            }
        }

        Ar.Position += 4;
        var folderNameLength = Ar.Read<int>();
        Ar.Position += folderNameLength;
        var packageFlags = Ar.Read<EPackageFlags>();
        var nameCount = Ar.Read<int>();
        var nameOffset = Ar.Read<int>();

        if (fileVersionUE >= EUnrealEngineObjectUE5Version.ADD_SOFTOBJECTPATH_LIST)
        {
            Ar.Position += 8;
        }

        if (!packageFlags.HasFlag(EPackageFlags.PKG_FilterEditorOnly))
        {
            if (fileVersionUE >= EUnrealEngineObjectUE4Version.ADDED_PACKAGE_SUMMARY_LOCALIZATION_ID)
            {
                var localizationIdLength = Ar.Read<int>();
                Ar.Position += localizationIdLength;
            }
        }

        if (fileVersionUE >= EUnrealEngineObjectUE4Version.SERIALIZE_TEXT_IN_PACKAGES)
        {
            Ar.Position += 8;
        }

        var exportCount = Ar.Read<int>();
        var exportOffset = Ar.Read<int>();

        Ar.Position = nameOffset;
        NameMap = new FNameEntrySerialized[nameCount];
        Ar.ReadArray(NameMap, () => new FNameEntrySerialized(Ar));

        Ar.Position = exportOffset;
        ExportMap = Ar.ReadArray(exportCount, () =>
        {
            var start = Ar.Position;
            var obj = new FObject();

            var classIndex = Ar.Read<int>();
            Ar.Position += 8 + (Ar.Ver >= EUnrealEngineObjectUE4Version.TemplateIndex_IN_COOKED_EXPORTS ? 4 : 0);

            obj.Name = Ar.ReadFName();

            return obj;
        });
    }
}
