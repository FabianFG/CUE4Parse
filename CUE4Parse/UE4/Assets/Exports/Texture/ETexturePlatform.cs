using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public enum ETexturePlatform
    {
        [Description("Desktop / Mobile")]
        DesktopMobile,
        [Description("Xbox One/Series / Playstation 4")]
        XboxAndPlaystation4,
        [Description("Nintendo Switch")]
        NintendoSwitch,
        [Description("Playstation 5")]
        Playstation5
    }
}
