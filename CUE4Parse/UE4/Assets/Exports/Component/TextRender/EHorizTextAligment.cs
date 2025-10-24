using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Component.TextRender;

public enum EHorizTextAligment : int
{
    [Description("Left")]
    EHTA_Left,
    
    [Description("Center")]
    EHTA_Center,
    
    [Description("Right")]
    EHTA_Right,
}