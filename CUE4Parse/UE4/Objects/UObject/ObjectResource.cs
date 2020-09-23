using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.UObject
{

    /// <summary>
    /// Wrapper for index into a ULinker's ImportMap or ExportMap.
    /// Values greater than zero indicate that this is an index into the ExportMap.  The
    /// actual array index will be (FPackageIndex - 1).
    ///
    /// Values less than zero indicate that this is an index into the ImportMap. The actual
    /// array index will be (-FPackageIndex - 1)
    /// </summary>
    public class FPackageIndex
    {
        /// <summary>
        /// Values greater than zero indicate that this is an index into the ExportMap.  The
        /// actual array index will be (FPackageIndex - 1).
        ///
        /// Values less than zero indicate that this is an index into the ImportMap. The actual
        /// array index will be (-FPackageIndex - 1)
        /// </summary>
        public readonly int Index;

        public readonly Package? Owner;

        public FObjectImport? ImportObject => Index < 0 ? Owner?.ImportMap[-Index - 1] : null;
        public FObjectImport? OuterImportObject => ImportObject?.OuterIndex?.ImportObject ?? ImportObject;
        
        public FObjectExport? ExportObject => Index > 0 ? Owner?.ExportMap[Index - 1] : null;
        
        public bool IsNull => Index == 0;
        public bool IsExport => Index > 0;
        public bool IsImport => Index < 0;

        public string Name => ImportObject?.ObjectName.Text
                              ?? ExportObject?.ObjectName.Text
                              ?? Index.ToString();

        public FPackageIndex(FAssetArchive Ar)
        {
            Index = Ar.Read<int>();
            Owner = Ar.Owner;
        }

        public FPackageIndex()
        {
            Index = 0;
            Owner = null;
        }

        public override string ToString()
        {
            return ImportObject?.ObjectName.Text.Insert(0, "Import: ")
                   ?? ExportObject?.ObjectName.Text.Insert(0, "Export: ")
                   ?? Index.ToString();
        }
    }
    
    
    /// <summary>
    /// Base class for UObject resource types.  FObjectResources are used to store UObjects on disk
    /// via FLinker's ImportMap (for resources contained in other packages) and ExportMap (for resources
    /// contained within the same package)
    /// </summary>
    public abstract class FObjectResource
    {
        public FName ObjectName;
        public FPackageIndex OuterIndex;

        public override string ToString()
        {
            return ObjectName.Text;
        }
    }

    public class FObjectExport : FObjectResource
    {
        public readonly FPackageIndex ClassIndex;
        public readonly FPackageIndex SuperIndex;
        public readonly FPackageIndex TemplateIndex;
        public readonly uint ObjectFlags;
        public readonly long SerialSize;
        public readonly long SerialOffset;
        public readonly bool ForcedExport;
        public readonly bool NotForClient;
        public readonly bool NotForServer;
        public readonly FGuid PackageGuid;
        public readonly uint PackageFlags;
        public readonly bool NotAlwaysLoadedForEditorGame;
        public readonly bool IsAsset;
        public readonly int FirstExportDependency;
        public readonly int SerializationBeforeSerializationDependencies;
        public readonly int CreateBeforeSerializationDependencies;
        public readonly int SerializationBeforeCreateDependencies;
        public readonly int CreateBeforeCreateDependencies;
        //TODO var exportObject: Lazy<UExport>

        public FObjectExport(FAssetArchive Ar)
        {
            ClassIndex = new FPackageIndex(Ar);
            SuperIndex = new FPackageIndex(Ar);
            TemplateIndex = Ar.Ver >= UE4Version.VER_UE4_TemplateIndex_IN_COOKED_EXPORTS ? new FPackageIndex(Ar) : new FPackageIndex();
            OuterIndex = new FPackageIndex(Ar);
            ObjectName = Ar.ReadFName();
            ObjectFlags = Ar.Read<uint>();

            if (Ar.Ver < UE4Version.VER_UE4_64BIT_EXPORTMAP_SERIALSIZES)
            {
                SerialSize = Ar.Read<int>();
                SerialOffset = Ar.Read<int>();
            }
            else
            {
                SerialSize = Ar.Read<long>();
                SerialOffset = Ar.Read<long>();
            }

            ForcedExport = Ar.ReadBoolean();
            NotForClient = Ar.ReadBoolean();
            NotForServer = Ar.ReadBoolean();
            PackageGuid = Ar.Read<FGuid>();
            PackageFlags = Ar.Read<uint>();
            NotAlwaysLoadedForEditorGame = Ar.Ver >= UE4Version.VER_UE4_LOAD_FOR_EDITOR_GAME ? Ar.ReadBoolean() : true;
            IsAsset = Ar.Ver >= UE4Version.VER_UE4_COOKED_ASSETS_IN_EDITOR_SUPPORT ? Ar.ReadBoolean() : false;

            if (Ar.Ver >= UE4Version.VER_UE4_PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS)
            {
                FirstExportDependency = Ar.Read<int>();
                SerializationBeforeSerializationDependencies = Ar.Read<int>();
                CreateBeforeSerializationDependencies = Ar.Read<int>();
                SerializationBeforeCreateDependencies = Ar.Read<int>();
                CreateBeforeCreateDependencies = Ar.Read<int>();
            }
            else
            {
                FirstExportDependency = -1;
                SerializationBeforeSerializationDependencies = 0;
                CreateBeforeSerializationDependencies = 0;
                SerializationBeforeCreateDependencies = 0;
                CreateBeforeCreateDependencies = 0;
            }
        }

        public override string ToString()
        {
            return $"{ObjectName.Text} ({ClassIndex.Name})";
        }
    }
    
    /// <summary>
    /// UObject resource type for objects that are referenced by this package, but contained
    /// within another package.
    /// </summary>
    public class FObjectImport : FObjectResource
    {
        public readonly FName ClassPackage;
        public readonly FName ClassName;

        public FObjectImport(FAssetArchive Ar)
        {
            ClassPackage = Ar.ReadFName();
            ClassName = Ar.ReadFName();
            OuterIndex = new FPackageIndex(Ar);
            ObjectName = Ar.ReadFName();
        }
    }
}