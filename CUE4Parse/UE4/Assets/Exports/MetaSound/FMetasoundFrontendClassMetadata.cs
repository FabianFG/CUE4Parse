using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClassMetadata
{
    public FMetasoundFrontendClassName ClassName;
    public FMetasoundFrontendVersionNumber Version;
    public EMetasoundFrontendClassType Type;
    public EMetasoundFrontendClassAccessFlags AccessFlags;
    
    public FMetasoundFrontendClassMetadata(FStructFallback fallback)
    {
        ClassName = fallback.GetOrDefault<FMetasoundFrontendClassName>(nameof(ClassName));
        Version = fallback.GetOrDefault<FMetasoundFrontendVersionNumber>(nameof(Version));
        Type = fallback.GetOrDefault<EMetasoundFrontendClassType>(nameof(EMetasoundFrontendClassType));
        AccessFlags = (EMetasoundFrontendClassAccessFlags) fallback.GetOrDefault<ushort>(nameof(AccessFlags));
    }
}

public enum EMetasoundFrontendClassType : byte
{
    // The MetaSound class is defined externally, in compiled code or in another document.
    External = 0,

    // The MetaSound class is a graph within the containing document.
    Graph,

    // The MetaSound class is an input into a graph in the containing document.
    Input,

    // The MetaSound class is an output from a graph in the containing document.
    Output,

    // The MetaSound class is an literal requiring a literal value to construct.
    Literal,

    // The MetaSound class is an variable requiring a literal value to construct.
    Variable,

    // The MetaSound class accesses variables.
    VariableDeferredAccessor,

    // The MetaSound class accesses variables.
    VariableAccessor,

    // The MetaSound class mutates variables.
    VariableMutator,

    // The MetaSound class is defined only by the Frontend, and associatively
    // performs a functional operation within the given document in a registration/cook step.
    Template,

    Invalid,
}

[Flags]
public enum EMetasoundFrontendClassAccessFlags : ushort
{
    None = 0,
    
    // Class is marked as deprecated when referenced by
    // MetaSounds in the editor.
    Deprecated = 1 << 0,

    // If set, MetaSound can be referenced by other MetaSounds in either
    // editor or by builder Blueprint API.
    Referenceable = 1 << 1,
    
    Default = Referenceable,
}