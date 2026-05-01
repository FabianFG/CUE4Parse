using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CUE4Parse_Conversion.USD;

public sealed class UsdaWriterOptions
{
    public string Indent { get; init; } = "    ";
    public bool IncludeTrailingNewline { get; init; } = true;
}

public static class UsdaWriter
{
    public static string Serialize(UsdStage stage, UsdaWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        options ??= new UsdaWriterOptions();

        var writer = new Builder(options);
        writer.WriteStage(stage);
        return writer.ToString();
    }

    private sealed class Builder(UsdaWriterOptions options)
    {
        private readonly StringBuilder _sb = new();
        private int _indentLevel;

        public override string ToString() => _sb.ToString();

        public void WriteStage(UsdStage stage)
        {
            Line($"#usda {UsdStage.Version}");

            if (stage.Metadata.Count > 0)
            {
                WriteMetadataBlock(stage.Metadata, prefixOnSameLine: false);
                Line();
            }

            for (var i = 0; i < stage.Prims.Count; i++)
            {
                WritePrim(stage.Prims[i]);
                if (i < stage.Prims.Count - 1)
                {
                    Line();
                }
            }

            if (options.IncludeTrailingNewline && (_sb.Length == 0 || _sb[^1] != '\n'))
            {
                _sb.AppendLine();
            }
        }

        private void WritePrim(UsdPrim prim)
        {
            Indent();
            _sb.Append(GetSpecifierText(prim.Specifier));
            _sb.Append(' ');
            _sb.Append(prim.TypeName);
            _sb.Append(' ');
            WriteQuotedString(prim.EffectiveName);

            var primMetadata = new List<UsdMetadata>(prim.Metadata);
            if (prim.References is { References.Count: > 0 })
            {
                primMetadata.Add(new UsdMetadata(GetReferenceMetadataName(prim.References.Operation), FormatReferenceList(prim.References.References)));
            }

            if (primMetadata.Count > 0)
            {
                _sb.Append(' ');
                WriteMetadataBlock(primMetadata, prefixOnSameLine: true);
            }
            else
            {
                _sb.AppendLine();
            }

            Indent();
            _sb.AppendLine("{");
            _indentLevel++;

            foreach (var property in prim.Properties)
            {
                WriteProperty(property);
            }

            for (var i = 0; i < prim.Children.Count; i++)
            {
                if (prim.Properties.Count > 0 || i > 0)
                {
                    Line();
                }
                WritePrim(prim.Children[i]);
            }

            _indentLevel--;
            Indent();
            _sb.Append('}');
            _sb.AppendLine();
        }

        private void WriteProperty(UsdProperty property)
        {
            switch (property)
            {
                case UsdAttribute attribute:
                    WriteAttribute(attribute);
                    break;
                case UsdRelationship relationship:
                    WriteRelationship(relationship);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported USD property type '{property.GetType().FullName}'.");
            }
        }

        private void WriteAttribute(UsdAttribute attribute)
        {
            Indent();
            if (attribute.Custom)
            {
                _sb.Append("custom ");
            }
            if (attribute.Variability is not null)
            {
                _sb.Append(GetVariabilityText(attribute.Variability.Value));
                _sb.Append(' ');
            }

            _sb.Append(attribute.TypeName);
            _sb.Append(' ');

            if (attribute.TimeSamples is { } samples)
            {
                _sb.Append(attribute.Name);
                _sb.AppendLine(".timeSamples = {");
                _indentLevel++;
                for (var i = 0; i < samples.Length; i++)
                {
                    Indent();
                    _sb.Append(i);
                    _sb.Append(": [");
                    for (var j = 0; j < samples[i].Length; j++)
                    {
                        if (j > 0) _sb.Append(", ");
                        WriteValue(samples[i][j]);
                    }
                    _sb.AppendLine("],");
                }
                _indentLevel--;
                Indent();
                _sb.Append('}');
            }
            else
            {
                _sb.Append(attribute.Name);
                _sb.Append(" = ");
                WriteValue(attribute.Value);
            }

            if (attribute.Metadata.Count > 0)
            {
                _sb.Append(' ');
                WriteMetadataBlock(attribute.Metadata, prefixOnSameLine: true, suppressTrailingNewline: true);
            }

            _sb.AppendLine();
        }

        private void WriteRelationship(UsdRelationship relationship)
        {
            Indent();
            if (relationship.Custom)
            {
                _sb.Append("custom ");
            }
            if (relationship.Variability is not null)
            {
                _sb.Append(GetVariabilityText(relationship.Variability.Value));
                _sb.Append(' ');
            }

            _sb.Append("rel ");
            _sb.Append(relationship.Name);

            _sb.Append(" = ");
            WriteRelationshipTargets(relationship.GetPaths());

            if (relationship.Metadata.Count > 0)
            {
                _sb.Append(' ');
                WriteMetadataBlock(relationship.Metadata, prefixOnSameLine: true, suppressTrailingNewline: true);
            }

            _sb.AppendLine();
        }

        private void WriteMetadataBlock(IReadOnlyList<UsdMetadata> metadata, bool prefixOnSameLine, bool suppressTrailingNewline = false)
        {
            if (!prefixOnSameLine)
            {
                Indent();
            }

            _sb.AppendLine("(");
            _indentLevel++;
            foreach (var entry in metadata)
            {
                Indent();
                _sb.Append(entry.Name);
                _sb.Append(" = ");
                WriteValue(entry.Value);
                _sb.AppendLine();
            }
            _indentLevel--;
            Indent();
            _sb.Append(')');
            if (!suppressTrailingNewline)
            {
                _sb.AppendLine();
            }
        }

        private void WriteRelationshipTargets(string[] targets)
        {
            if (targets.Length == 1)
            {
                WritePath(targets[0]);
                return;
            }

            _sb.Append('[');
            for (var i = 0; i < targets.Length; i++)
            {
                if (i > 0) _sb.Append(", ");
                WritePath(targets[i]);
            }
            _sb.Append(']');
        }

        private static UsdValue FormatReferenceList(IReadOnlyList<UsdReference> references)
        {
            if (references.Count == 1)
            {
                return UsdValue.Raw(FormatReference(references[0]));
            }

            return UsdValue.Array(references.Select(reference => UsdValue.Raw(FormatReference(reference))).ToArray());
        }

        private static string FormatReference(UsdReference reference)
        {
            var asset = '@' + EscapeAssetPath(reference.AssetPath) + '@';
            return string.IsNullOrWhiteSpace(reference.PrimPath)
                ? asset
                : asset + NormalizePath(reference.PrimPath!);
        }

        private void WriteValue(UsdValue value)
        {
            switch (value.Kind)
            {
                case UsdValueKind.None:
                    _sb.Append("None");
                    break;
                case UsdValueKind.Raw:
                    _sb.Append(value.AsString());
                    break;
                case UsdValueKind.Bool:
                case UsdValueKind.Int:
                case UsdValueKind.Long:
                case UsdValueKind.Float:
                case UsdValueKind.Double:
                    _sb.Append(value.FormatScalarInvariant());
                    break;
                case UsdValueKind.String:
                case UsdValueKind.Token:
                    WriteQuotedString(value.AsString());
                    break;
                case UsdValueKind.Path:
                    WritePath(value.AsString());
                    break;
                case UsdValueKind.AssetPath:
                    _sb.Append('@');
                    _sb.Append(EscapeAssetPath(value.AsString()));
                    _sb.Append('@');
                    break;
                case UsdValueKind.Array:
                    WriteDelimited(value.AsValues(), '[', ']');
                    break;
                case UsdValueKind.Tuple:
                    WriteDelimited(value.AsValues(), '(', ')');
                    break;
                default:
                    throw new NotSupportedException($"Unsupported USD value kind '{value.Kind}'.");
            }
        }

        private void WriteDelimited(IReadOnlyList<UsdValue> values, char open, char close)
        {
            _sb.Append(open);
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    _sb.Append(", ");
                }
                WriteValue(values[i]);
            }
            _sb.Append(close);
        }

        private void WritePath(string path)
        {
            _sb.Append('<');
            _sb.Append(NormalizePath(path));
            _sb.Append('>');
        }

        private void WriteQuotedString(string value)
        {
            _sb.Append('"');
            foreach (var c in value)
            {
                _sb.Append(c switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\r' => "\\r",
                    '\n' => "\\n",
                    '\t' => "\\t",
                    _ => c.ToString()
                });
            }
            _sb.Append('"');
        }

        private static string EscapeAssetPath(string path)
        {
            return path.Replace("@", "\\@");
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            return path[0] == '/' ? path : '/' + path;
        }

        private static string GetSpecifierText(UsdPrimSpecifier specifier) => specifier switch
        {
            UsdPrimSpecifier.Def => "def",
            UsdPrimSpecifier.Over => "over",
            UsdPrimSpecifier.Class => "class",
            _ => throw new ArgumentOutOfRangeException(nameof(specifier), specifier, null)
        };

        private static string GetVariabilityText(UsdVariability variability) => variability switch
        {
            UsdVariability.Uniform => "uniform",
            UsdVariability.Config => "config",
            UsdVariability.Varying => "varying",
            _ => throw new ArgumentOutOfRangeException(nameof(variability), variability, null)
        };

        private static string GetListOpText(UsdListOpType operation) => operation switch
        {
            UsdListOpType.Explicit => "references",
            UsdListOpType.Add => "add",
            UsdListOpType.Append => "append",
            UsdListOpType.Delete => "delete",
            UsdListOpType.Order => "reorder",
            UsdListOpType.Prepend => "prepend",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        private static string GetReferenceMetadataName(UsdListOpType operation)
        {
            return operation == UsdListOpType.Explicit
                ? "references"
                : GetListOpText(operation) + " references";
        }

        private void Indent()
        {
            for (var i = 0; i < _indentLevel; i++)
            {
                _sb.Append(options.Indent);
            }
        }

        private void Line(string text = "")
        {
            Indent();
            _sb.AppendLine(text);
        }
    }
}
