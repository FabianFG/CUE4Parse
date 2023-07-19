using System;

namespace CUE4Parse.UE4.Objects.UObject
{
    /** Package flags, passed into UPackage::SetPackageFlags and related functions */
    [Flags]
    public enum EPackageFlags : uint
    {
        PKG_None						= 0x00000000,  ///< No flags
        PKG_NewlyCreated				= 0x00000001,  ///< Newly created package, not saved yet. In editor only.
        PKG_ClientOptional				= 0x00000002,  ///< Purely optional for clients.
        PKG_ServerSideOnly				= 0x00000004,  ///< Only needed on the server side.
        PKG_CompiledIn					= 0x00000010,  ///< This package is from "compiled in" classes.
        PKG_ForDiffing					= 0x00000020,  ///< This package was loaded just for the purposes of diffing
        PKG_EditorOnly					= 0x00000040,  ///< This is editor-only package (for example: editor module script package)
        PKG_Developer					= 0x00000080,  ///< Developer module
        PKG_UncookedOnly				= 0x00000100,  ///< Loaded only in uncooked builds (i.e. runtime in editor)
        PKG_Cooked						= 0x00000200,  ///< Package is cooked
        PKG_ContainsNoAsset				= 0x00000400,  ///< Package doesn't contain any asset object (although asset tags can be present)
    //  PKG_Unused						= 0x00000800,
    //  PKG_Unused						= 0x00001000,
        PKG_UnversionedProperties		= 0x00002000,  ///< Uses unversioned property serialization instead of versioned tagged property serialization
        PKG_ContainsMapData				= 0x00004000,  ///< Contains map data (UObjects only referenced by a single ULevel) but is stored in a different package
    //  PKG_Unused						= 0x00008000,
        PKG_Compiling					= 0x00010000,  ///< package is currently being compiled
        PKG_ContainsMap					= 0x00020000,  ///< Set if the package contains a ULevel/ UWorld object
        PKG_RequiresLocalizationGather	= 0x00040000,  ///< Set if the package contains any data to be gathered by localization
    //  PKG_Unused						= 0x00080000,
        PKG_PlayInEditor				= 0x00100000,  ///< Set if the package was created for the purpose of PIE
        PKG_ContainsScript				= 0x00200000,  ///< Package is allowed to contain UClass objects
        PKG_DisallowExport				= 0x00400000,  ///< Editor should not export asset in this package
    //  PKG_Unused						= 0x00800000,
    //  PKG_Unused						= 0x01000000,
    //  PKG_Unused						= 0x02000000,
    //  PKG_Unused						= 0x04000000,
    //  PKG_Unused						= 0x08000000,
        PKG_DynamicImports				= 0x10000000,  ///< This package should resolve dynamic imports from its export at runtime.
        PKG_RuntimeGenerated			= 0x20000000,  ///< This package contains elements that are runtime generated, and may not follow standard loading order rules
        PKG_ReloadingForCooker			= 0x40000000,  ///< This package is reloading in the cooker, try to avoid getting data we will never need. We won't save this package.
        PKG_FilterEditorOnly			= 0x80000000,  ///< Package has editor-only data filtered out
    }
}