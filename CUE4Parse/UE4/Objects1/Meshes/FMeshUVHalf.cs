namespace CUE4Parse.UE4.Objects.Meshes
{
    public struct FMeshUVHalf : IUStruct
    {
        public readonly ushort U;
        public readonly ushort V;

        public FMeshUVHalf(ushort u, ushort v)
        {
            U = u;
            V = v;
        }
    }
}