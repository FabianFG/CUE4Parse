using System.Diagnostics;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace CUE4Parse.UE4.Plugins
{
    [DebuggerDisplay("{" + nameof(Amount) + "}")]
    public class UPluginManifest
    {
        [J] public UPluginContents[] Contents { get; set; }

        public int Amount => Contents.Length;
    }

    [DebuggerDisplay("{" + nameof(File) + "}")]
    public class UPluginContents
    {
        [J] public string File { get; set; }
        [J] public UPluginDescriptor Descriptor { get; set; }
    }

    [DebuggerDisplay("{" + nameof(CanContainContent) + "}")]
    public class UPluginDescriptor
    {
        [J] public bool CanContainContent { get; set; }
    }
}