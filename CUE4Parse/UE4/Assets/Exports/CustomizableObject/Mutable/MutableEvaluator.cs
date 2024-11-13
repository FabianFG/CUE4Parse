using System.Collections.Generic;
using System.Linq;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class MutableEvaluator
{
    public UCustomizableObject CustomizableObject;
    public IFileProvider Provider;
    private FProgram Program => CustomizableObject.Model.Program;
    private Dictionary<uint, FMutableStreamableBlock> ModelStreamables = [];
    private Dictionary<uint, FArchive> BulkReaders = [];

    public MutableEvaluator(IFileProvider provider, UCustomizableObject customizableObject)
    {
        Provider = provider;
        CustomizableObject = customizableObject;
    }

    public void LoadModelStreamable()
    {
	    var customizableObjectPrivate = CustomizableObject.Get<FPackageIndex>("Private").Load();
	    var modelStreamableData = customizableObjectPrivate?.Get<FPackageIndex>("ModelStreamableData").Load() as UModelStreamableData;
	    ModelStreamables = modelStreamableData!.StreamingData.ModelStreamables.ToDictionary();
    }

    public FMesh LoadResource(int resourceIndex)
    {
	    var dataType = DataType.DT_MESH;

	    var rom = Program.Roms.First(rom => rom.ResourceIndex == resourceIndex && rom.ResourceType == dataType);
	    var block = ModelStreamables[rom.Id];
	    if (!BulkReaders.TryGetValue(block.FileId, out var reader))
	    {
		    reader = Provider.CreateReader(GetMutableBulkDataPath(block.FileId));
		    BulkReaders[block.FileId] = reader;
	    }

	    reader.Position = (long) block.Offset;
	    return new FMesh(reader);
    }

    public void ReadByteCode()
    {
	    var bytecodeReader = new FByteArchive("Mutable ByteCode", Program.ByteCode);
	    foreach (var address in Program.OpAddress)
	    {
		    bytecodeReader.Position = address;

		    var opcodeType = bytecodeReader.Read<OP_TYPE>();
		    Log.Information(opcodeType.ToString());
	    }
    }

    private string GetMutableBulkDataPath(ulong fileId)
    {
	    var coPath = CustomizableObject.GetPathName();
	    var baseDir = coPath.SubstringBeforeWithLast('/');
	    return $"{baseDir}{CustomizableObject.Name}-{fileId:x8}.mut";
    }
}

public enum OP_TYPE : ushort
{
    //-----------------------------------------------------------------------------------------
        // No operation
        //-----------------------------------------------------------------------------------------
        NONE,

        //-----------------------------------------------------------------------------------------
        // Generic operations
        //-----------------------------------------------------------------------------------------

        //! Constant value
        BO_CONSTANT,
        NU_CONSTANT,
        SC_CONSTANT,
        CO_CONSTANT,
        IM_CONSTANT,
        ME_CONSTANT,
        LA_CONSTANT,
        PR_CONSTANT,
        ST_CONSTANT,
		ED_CONSTANT,
    	MA_CONSTANT,

        //! User parameter
        BO_PARAMETER,
        NU_PARAMETER,
        SC_PARAMETER,
        CO_PARAMETER,
        PR_PARAMETER,
        IM_PARAMETER,
        ST_PARAMETER,
    	MA_PARAMETER,

		//! A referenced, but opaque engine resource
		IM_REFERENCE,
		ME_REFERENCE,

        //! Select one value or the other depending on a boolean input
        NU_CONDITIONAL,
        SC_CONDITIONAL,
        CO_CONDITIONAL,
        IM_CONDITIONAL,
        ME_CONDITIONAL,
        LA_CONDITIONAL,
        IN_CONDITIONAL,
		ED_CONDITIONAL,

        //! Select one of several values depending on an int input
        NU_SWITCH,
        SC_SWITCH,
        CO_SWITCH,
        IM_SWITCH,
        ME_SWITCH,
        LA_SWITCH,
        IN_SWITCH,
		ED_SWITCH,

        //-----------------------------------------------------------------------------------------
        // Boolean operations
        //-----------------------------------------------------------------------------------------

        //! Compare two scalars, return true if the first is less than the second.
		//! \TODO: Deprecated?
		BO_LESS,

        //! Compare an integerexpression with an integer constant
        BO_EQUAL_INT_CONST,

        //! Logical and
        BO_AND,

        //! Logical or
        BO_OR,

        //! Left as an exercise to the reader to find out what this op does.
        BO_NOT,

        //-----------------------------------------------------------------------------------------
        // Integer operations
        //-----------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------
        // Scalar operations
        //-----------------------------------------------------------------------------------------

        //! Multiply a scalar value by another onw and add a third one to the result
		//! \TODO: Deprecated?
		SC_MULTIPLYADD,

        //! Apply an arithmetic operation to two scalars
        SC_ARITHMETIC,

        //! Get a scalar value from a curve
        SC_CURVE,

        //-----------------------------------------------------------------------------------------
        // Colour operations. Colours are sometimes used as generic vectors.
        //-----------------------------------------------------------------------------------------

        //! Sample an image to get its colour.
        CO_SAMPLEIMAGE,

        //! Make a color by shuffling channels from other colours.
        CO_SWIZZLE,

        //! Compose a vector from 4 scalars
        CO_FROMSCALARS,

        //! Apply component-wise arithmetic operations to two colours
        CO_ARITHMETIC,

        //-----------------------------------------------------------------------------------------
        // Image operations
        //-----------------------------------------------------------------------------------------

        //! Combine an image on top of another one using a specific effect (Blend, SoftLight,
		//! Hardlight, Burn...). And optionally a mask.
        IM_LAYER,

        //! Apply a colour on top of an image using a specific effect (Blend, SoftLight,
		//! Hardlight, Burn...), optionally using a mask.
        IM_LAYERCOLOUR,

        //! Convert between pixel formats
        IM_PIXELFORMAT,

        //! Generate mipmaps up to a provided level
        IM_MIPMAP,

        //! Resize the image to a constant size
        IM_RESIZE,

        //! Resize the image to the size of another image
        IM_RESIZELIKE,

        //! Resize the image by a relative factor
        IM_RESIZEREL,

        //! Create an empty image to hold a particular layout.
        IM_BLANKLAYOUT,

        //! Copy an image into a rect of another one.
        IM_COMPOSE,

        //! Interpolate between 2 images taken from a row of targets (2 consecutive targets).
        IM_INTERPOLATE,

        //! Change the saturation of the image.
        IM_SATURATE,

        //! Generate a one-channel image with the luminance of the source image.
        IM_LUMINANCE,

        //! Recombine the channels of several images into one.
        IM_SWIZZLE,

        //! Convert the source image colours using a "palette" image sampled with the source
        //! grey-level.
        IM_COLOURMAP,

        //! Build a horizontal gradient image from two colours
        IM_GRADIENT,

        //! Generate a black and white image from an image and a threshold.
        IM_BINARISE,

        //! Generate a plain colour image
        IM_PLAINCOLOUR,

        //! Cut a rect from an image
        IM_CROP,

        //! Replace a subrect of an image with another one
        IM_PATCH,

        //! Render a mesh texture layout into a mask
        IM_RASTERMESH,

        //! Create an image displacement encoding the grow operation for a mask
        IM_MAKEGROWMAP,

        //! Apply an image displacement on another image.
        IM_DISPLACE,

        //! Repeately apply
        IM_MULTILAYER,

        //! Inverts the colors of an image
        IM_INVERT,

        //! Modifiy roughness channel of an image based on normal variance.
        IM_NORMALCOMPOSITE,

		//! Apply linear transform to Image content. Resulting samples outside the original image are tiled.
		IM_TRANSFORM,

        //-----------------------------------------------------------------------------------------
        // Mesh operations
        //-----------------------------------------------------------------------------------------

        //! Apply a layout to a mesh texture coordinates channel
        ME_APPLYLAYOUT,

        //! Compare two meshes and extract a morph from the first to the second
        //! The meshes must have the same topology, etc.
        ME_DIFFERENCE,

        //! Apply a one morphs on a base.
        ME_MORPH,

        //! Merge a mesh to a mesh
        ME_MERGE,

        //! Interpolate between several meshes.
        ME_INTERPOLATE,

        //! Create a new mask mesh selecting all the faces of a source that are inside a given
        //! clip mesh.
        ME_MASKCLIPMESH,

        /** Create a new mask mesh selecting the faces of a source that have UVs inside the region marked in an image mask. */
		ME_MASKCLIPUVMASK,

        //! Create a new mask mesh selecting all the faces of a source that match another mesh.
        ME_MASKDIFF,

        //! Remove all the geometry selected by a mask.
        ME_REMOVEMASK,

        //! Change the mesh format to match the format of another one.
        ME_FORMAT,

        //! Extract a fragment of a mesh containing specific layout blocks.
        ME_EXTRACTLAYOUTBLOCK,

        //! Apply a transform in a 4x4 matrix to the geometry channels of the mesh
        ME_TRANSFORM,

        //! Clip the mesh with a plane and morph it when it is near until it becomes an ellipse on
        //! the plane.
        ME_CLIPMORPHPLANE,

        //! Clip the mesh with another mesh.
        ME_CLIPWITHMESH,

        //! Replace the skeleton data from a mesh with another one.
        ME_SETSKELETON,

        //! Project a mesh using a projector and clipping the irrelevant faces
        ME_PROJECT,

        //! Deform a skinned mesh applying a skeletal pose
        ME_APPLYPOSE,

		//! Apply a geometry core operation to a mesh.
		//! \TODO: Deprecated?
		ME_GEOMETRYOPERATION,

		//! Calculate the binding of a mesh on a shape
		ME_BINDSHAPE,

		//! Apply a shape on a (previously bound) mesh
		ME_APPLYSHAPE,

		//! Clip Deform using bind data.
		ME_CLIPDEFORM,

        //! Mesh morph with Skeleton Reshape based on the morphed mesh.
        ME_MORPHRESHAPE,

		//! Optimize skinning before adding a mesh to the component
		ME_OPTIMIZESKINNING,

		//! Add a set of tags to a mesh
		ME_ADDTAGS,

    	//! Transform with a 4x4 matrix the geometry channels of a mesh that are bounded by another mesh
    	ME_TRANSFORMWITHMESH,

        //-----------------------------------------------------------------------------------------
        // Instance operations
        //-----------------------------------------------------------------------------------------

        //! Add a mesh to an instance
        IN_ADDMESH,

        //! Add an image to an instance
        IN_ADDIMAGE,

        //! Add a vector to an instance
        IN_ADDVECTOR,

        //! Add a scalar to an instance
        IN_ADDSCALAR,

        //! Add a string to an instance
        IN_ADDSTRING,

        //! Add a surface to an instance component
        IN_ADDSURFACE,

        //! Add a component to an instance LOD
        IN_ADDCOMPONENT,

        //! Add all LODs to an instance. This operation can only appear once in a model.
        IN_ADDLOD,

		//! Add extension data to an instance
		IN_ADDEXTENSIONDATA,

        //-----------------------------------------------------------------------------------------
        // Layout operations
        //-----------------------------------------------------------------------------------------

        //! Pack all the layout blocks from the source in the grid without overlapping
        LA_PACK,

        //! Merge two layouts
        LA_MERGE,

        //! Remove all layout blocks not used by any vertex of the mesh.
        //! This operation is for the new way of managing layout blocks.
        LA_REMOVEBLOCKS,

		//! Extract a layout from a mesh
		LA_FROMMESH,

        //-----------------------------------------------------------------------------------------
        // Utility values
        //-----------------------------------------------------------------------------------------

        //!
        COUNT
}
