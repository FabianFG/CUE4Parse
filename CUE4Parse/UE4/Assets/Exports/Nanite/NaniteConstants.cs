using System;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public static class NaniteConstants
{
    public const ushort NANITE_FIXUP_MAGIC = 0x464E;

    public const int NANITE_MAX_BVH_NODE_FANOUT_BITS = 2;
    public const int NANITE_MAX_BVH_NODE_FANOUT = 1 << NANITE_MAX_BVH_NODE_FANOUT_BITS;

    public const int NANITE_MAX_CLUSTERS_PER_GROUP_BITS = 9;
    public const int NANITE_MAX_RESOURCE_PAGES_BITS = 20;

    /// <summary>The maximum amount of UVs a nanite mesh can have</summary>
    public const int NANITE_MAX_UVS = 4;
    /// <summary>The maximum number of bits used to serialize normals before 5.2.</summary>
    public const int NANITE_MAX_NORMAL_QUANTIZATION_BITS_500 = 9;
    /// <summary>The maximum number of bits used to serialize normals after 5.2.</summary>
    public const int NANITE_MAX_NORMAL_QUANTIZATION_BITS_502 = 15;
    /// <summary>The maximum number of bits used to serialize tangents.</summary>
    public const int NANITE_MAX_TANGENT_QUANTIZATION_BITS = 12;
    /// <summary>The maximum number of bits used to serialize an axis in a UV.</summary>
    public const int NANITE_MAX_TEXCOORD_QUANTIZATION_BITS_500 = 15;
    public const int NANITE_MAX_TEXCOORD_QUANTIZATION_BITS_504 = 20;
    /// <summary>The maximum number of bits used to serialize a color channel for a vertex color.</summary>
    public const int NANITE_MAX_COLOR_QUANTIZATION_BITS = 8;

    /// <summary>The maximum amount of clusters that can be contained in a page.</summary>
    public const int NANITE_MAX_CLUSTERS_PER_PAGE_BITS_500 = 8;
    public const int NANITE_MAX_CLUSTERS_PER_PAGE_BITS_504 =
        NANITE_STREAMING_PAGE_MAX_CLUSTERS_BITS > NANITE_ROOT_PAGE_MAX_CLUSTERS_BITS
            ? NANITE_STREAMING_PAGE_MAX_CLUSTERS_BITS
            : NANITE_ROOT_PAGE_MAX_CLUSTERS_BITS;

    public const int NANITE_ROOT_PAGE_GPU_SIZE_BITS = 15;
    public const int NANITE_ROOT_PAGE_GPU_SIZE = (int) (1u << NANITE_ROOT_PAGE_GPU_SIZE_BITS);
    public const int NANITE_ROOT_PAGE_MAX_CLUSTERS_BITS = (NANITE_ROOT_PAGE_GPU_SIZE_BITS - NANITE_CLUSTER_MIN_EXPECTED_GPU_SIZE_BITS);
    public const int NANITE_ROOT_PAGE_MAX_CLUSTERS = (int) (1u << NANITE_ROOT_PAGE_MAX_CLUSTERS_BITS);

    public const int NANITE_STREAMING_PAGE_GPU_SIZE_BITS = 17;
    public const int NANITE_STREAMING_PAGE_GPU_SIZE = (int)(1u << NANITE_STREAMING_PAGE_GPU_SIZE_BITS);
    public const int NANITE_CLUSTER_MIN_EXPECTED_GPU_SIZE_BITS = 9;	// Used to determine how many bits to allocate for page cluster count.
    public const int NANITE_STREAMING_PAGE_MAX_CLUSTERS_BITS = (NANITE_STREAMING_PAGE_GPU_SIZE_BITS - NANITE_CLUSTER_MIN_EXPECTED_GPU_SIZE_BITS);
    public const int NANITE_STREAMING_PAGE_MAX_CLUSTERS = (int) (1u << NANITE_STREAMING_PAGE_MAX_CLUSTERS_BITS);

    public const int NANITE_MAX_CLUSTER_INDICES_BITS = 8;
    /// <summary>The maximum amount of tri indices that can be contained in a cluster.</summary>
    public const int NANITE_MAX_CLUSTER_INDICES = 1 << NANITE_MAX_CLUSTER_INDICES_BITS;
    public const int NANITE_MAX_CLUSTER_INDICES_MASK = NANITE_MAX_CLUSTER_INDICES - 1;

    public const int NANITE_MAX_HIERACHY_CHILDREN_BITS = 6;
    public const int NANITE_MAX_GROUP_PARTS_BITS = 3;
    public const int NANITE_MAX_HIERACHY_CHILDREN = (1 << NANITE_MAX_HIERACHY_CHILDREN_BITS);
    public const int NANITE_MAX_GROUP_PARTS_MASK = ((1 << NANITE_MAX_GROUP_PARTS_BITS) - 1);

    public const int NANITE_UV_FLOAT_NUM_EXPONENT_BITS = 5;
    public const int NANITE_UV_FLOAT_MAX_MANTISSA_BITS = 14;
    public const int NANITE_UV_FLOAT_NUM_MANTISSA_BITS = 14;		// TODO: Make this a runtime mesh setting. If it was dynamic, we could probably lower the default.
    public const int NANITE_UV_FLOAT_MAX_BITS = (1 + NANITE_UV_FLOAT_NUM_EXPONENT_BITS + NANITE_UV_FLOAT_MAX_MANTISSA_BITS);

    // before 5.4
    public const int NANITE_MIN_POSITION_PRECISION_500 = -8;
    public const int NANITE_MAX_POSITION_PRECISION_500 = 23;
    // 5.4+
    public const int NANITE_MIN_POSITION_PRECISION_504 = -20;
    public const int NANITE_MAX_POSITION_PRECISION_504 = 43;

    public const int NANITE_VERTEX_COLOR_MODE_VARIABLE = 1;

    [Flags]
    public enum NANITE_CLUSTER_FLAG : uint
    {
        NONE = 				 		0x0,
        ROOT_LEAF = 				0x1,		// Cluster is leaf when only root pages are streamed in
        STREAMING_LEAF =			0x2,		// Cluster is a leaf in the current streaming state
        FULL_LEAF =					0x4,		// Cluster is a leaf when fully streamed in
        ROOT_GROUP =				0x8,		// Cluster is in a group that is fully inside the root pages
    }
    [Flags]
    public enum NANITE_RESOURCE_FLAG : uint
    {
        NONE =  					0x0,
        HAS_VERTEX_COLOR =			0x1,
        HAS_IMPOSTER =				0x2,
        STREAMING_DATA_IN_DDC =		0x4,
        FORCE_ENABLED =				0x8,
    }

    [Flags]
    public enum NANITE_PAGE_FLAG : uint
    {
        NONE = 				 		0x0,
        RELATIVE_ENCODING = 		0x1,
    }
}
