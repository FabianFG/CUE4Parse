using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Component.TextRender;

public enum EVerticalTextAligment : int
{
    [Description("Text Top")]
    EVRTA_TextTop,
    
    [Description("Text Center")]
    EVRTA_TextCenter,
    
    [Description("Text Bottom")]
    EVRTA_TextBottom,
    
    [Description("Quad Top")]
    EVRTA_QuadTop,
}