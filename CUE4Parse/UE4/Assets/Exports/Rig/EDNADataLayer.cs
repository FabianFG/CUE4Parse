namespace CUE4Parse.UE4.Assets.Exports.Rig
{
    public enum EDNADataLayer : byte
    {
        Descriptor,
        Definition,  // Includes Descriptor
        Behavior,  // Includes Descriptor and Definition
        Geometry,  // Includes Descriptor and Definition
        GeometryWithoutBlendShapes,  // Includes Descriptor and Definition
        AllWithoutBlendShapes,  // Includes everything except blend shapes from Geometry
        All
    }
}
