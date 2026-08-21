using System.Text;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.Borderlands4.Assets.Objects;
using CUE4Parse.GameTypes.FN.Assets.Exports.CommonUI;
using CUE4Parse.MappingsProvider;
using CUE4Parse.MappingsProvider.Jmap;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.MovieScene.Evaluation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.Tests;

public class LongIdentifierMappingsTests
{
    [Fact]
    public void JmapPreservesCompleteIdentifiersInsteadOfCollapsingTheirLeafNames()
    {
        var data = Encoding.UTF8.GetBytes("""
            {"objects":{
              "/Game/A.B":{"type":"ScriptStruct","properties":[{"name":"FromA","type":"IntProperty"}]},
              "/Game/X.B":{"type":"ScriptStruct","properties":[{"name":"FromX","type":"FloatProperty"}]}
            }}
            """);
        var mappings = Assert.IsType<TypeMappings>(new JmapParser(data).Mappings);

        Assert.True(mappings.UsesFullTypeIdentifiers);
        Assert.True(mappings.TryGetType("B", "/Game/A.B", out var fromA));
        Assert.True(mappings.TryGetType("B", "/Game/X.B", out var fromX));
        Assert.False(mappings.TryGetType("B", null, out _));
        Assert.Equal("FromA", Assert.Single(fromA.Properties.Values).Name);
        Assert.Equal("FromX", Assert.Single(fromX.Properties.Values).Name);
    }

    [Fact]
    public void JmapRetainsLegacyShortIdentifierCompatibility()
    {
        var data = Encoding.UTF8.GetBytes("""
            {"objects":{
              "Legacy.PackageType":{"type":"ScriptStruct","properties":[{"name":"Value","type":"IntProperty"}]}
            }}
            """);
        var mappings = Assert.IsType<TypeMappings>(new JmapParser(data).Mappings);

        Assert.False(mappings.UsesFullTypeIdentifiers);
        Assert.True(mappings.TryGetType("PackageType", null, out var value));
        Assert.Equal("Value", Assert.Single(value.Properties.Values).Name);
    }

    [Fact]
    public void JsonMappingsRejectDuplicateCompleteIdentifiersBeforeOverwrite()
    {
        var typeProvider = new MappingJsonProvider();
        Assert.Throws<ArgumentException>(() => typeProvider.AddTypes("""
            [
              {"name":"/Game/A.Payload","properties":[],"propertyCount":0},
              {"name":"/Game/A.Payload","properties":[],"propertyCount":0}
            ]
            """));

        var enumProvider = new MappingJsonProvider();
        Assert.Throws<ArgumentException>(() => enumProvider.AddEnumValues("""
            [
              {"name":"/Script/A.EState","values":["A"]},
              {"name":"/Script/A.EState","values":["B"]}
            ]
            """));
    }

    [Fact]
    public void SameLeafNameResolvesByFullObjectIdentifier()
    {
        var parser = new UsmapParser(BuildUsmap(
            ("/Game/A.B", "FromA", EPropertyType.IntProperty),
            ("/Game/X.B", "FromX", EPropertyType.FloatProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        Assert.True(mappings.UsesFullTypeIdentifiers);
        Assert.False(mappings.Types.ContainsKey("B"));
        Assert.True(mappings.TryGetType("B", "/Game/A.B", out var fromA));
        Assert.True(mappings.TryGetType("B", "/Game/X.B", out var fromX));
        Assert.False(mappings.TryGetType("B", null, out _));
        Assert.Equal("FromA", Assert.Single(fromA.Properties.Values).Name);
        Assert.Equal("FromX", Assert.Single(fromX.Properties.Values).Name);
    }

    [Fact]
    public void LongFNamePreservesIdentifiersLargerThanOneByteLength()
    {
        var identifier = "/Game/" + new string('A', 300) + ".Payload";
        var mappings = Assert.IsType<TypeMappings>(new UsmapParser(BuildUsmap(
            (identifier, "Value", EPropertyType.IntProperty))).Mappings);

        Assert.True(Encoding.UTF8.GetByteCount(identifier) > byte.MaxValue);
        Assert.True(mappings.TryGetType("Payload", identifier, out var schema));
        Assert.Equal(identifier, schema.Name);
        Assert.Equal("Value", Assert.Single(schema.Properties.Values).Name);
    }

    [Fact]
    public void FunctionOwnedIdentifierUsesSubobjectPathForLookup()
    {
        const string typeIdentifier = "/Script/B.Owner:Nested";
        var parser = new UsmapParser(BuildUsmap(
            ("/Script/A.Owner:Nested", "FromA", EPropertyType.IntProperty),
            (typeIdentifier, "FromB", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        Assert.Equal("Nested", TypeMappings.GetShortTypeName(typeIdentifier));
        using var archive = BuildUnversionedArchive(mappings, writer => writer.Write(314));
        var value = new FScriptStruct(archive, typeIdentifier, null, ReadType.NORMAL);
        var property = Assert.Single(Assert.IsType<FStructFallback>(value.StructType).Properties);
        Assert.Equal("FromB", property.Name.Text);
        Assert.Equal(314, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Fact]
    public void LongMappingRejectsLegacyShortOnlyCallersEvenWhenLeafIsUnique()
    {
        var parser = new UsmapParser(BuildUsmap(
            ("/Game/A.UniquePayload", "Value", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        Assert.False(mappings.TryGetType("UniquePayload", null, out _));
        Assert.True(mappings.TryResolveUniqueTypeIdentifier("UniquePayload", out var identifier));
        Assert.Equal("/Game/A.UniquePayload", identifier);
        Assert.False(mappings.TryResolveUniqueTypeIdentifier("/Game/X.UniquePayload", out _));
        Assert.True(mappings.TryGetType("UniquePayload", "/Game/A.UniquePayload", out var schema));
        Assert.Equal("/Game/A.UniquePayload", schema.Name);
        Assert.False(mappings.TryGetType("UniquePayload", "/Game/X.UniquePayload", out _));
        Assert.False(mappings.TryGetType("WrongLeaf", "/Game/A.UniquePayload", out _));
        Assert.False(mappings.TryGetType("/Game/X.UniquePayload", "/Game/A.UniquePayload", out _));
    }

    [Fact]
    public void CookedStructNamedLikeBuiltInUsesItsExactSchema()
    {
        var parser = new UsmapParser(BuildUsmap(
            ("/Game/A.Vector", "FromA", EPropertyType.IntProperty),
            ("/Game/X.Vector", "FromX", EPropertyType.FloatProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var intArchive = BuildUnversionedArchive(mappings, writer => writer.Write(1234));
        var fromA = new FScriptStruct(intArchive, "/Game/A.Vector", null, ReadType.NORMAL);
        var fromAFallback = Assert.IsType<FStructFallback>(fromA.StructType);
        var fromAProperty = Assert.Single(fromAFallback.Properties);
        Assert.Equal("FromA", fromAProperty.Name.Text);
        Assert.Equal(1234, Assert.IsType<IntProperty>(fromAProperty.Tag).Value);

        using var floatArchive = BuildUnversionedArchive(mappings, writer => writer.Write(1.5f));
        var fromX = new FScriptStruct(floatArchive, "/Game/X.Vector", null, ReadType.NORMAL);
        var fromXFallback = Assert.IsType<FStructFallback>(fromX.StructType);
        var fromXProperty = Assert.Single(fromXFallback.Properties);
        Assert.Equal("FromX", fromXProperty.Name.Text);
        Assert.Equal(1.5f, Assert.IsType<FloatProperty>(fromXProperty.Tag).Value);
    }

    [Fact]
    public void UniqueLeafOnlyStructWireIsUpgradedBeforeNativeDispatch()
    {
        var mappings = Assert.IsType<TypeMappings>(new UsmapParser(BuildUsmap(
            ("/Game/A.Vector", "Value", EPropertyType.IntProperty))).Mappings);

        using var archive = BuildUnversionedArchive(mappings, writer => writer.Write(55));
        var value = new FScriptStruct(archive, "Vector", null, ReadType.NORMAL);
        var fallback = Assert.IsType<FStructFallback>(value.StructType);
        Assert.Equal("Value", Assert.Single(fallback.Properties).Name.Text);
    }

    [Fact]
    public void AmbiguousLeafOnlyStructWireNeverEntersNativeDispatch()
    {
        var mappings = Assert.IsType<TypeMappings>(new UsmapParser(BuildUsmap(
            ("/Game/A.Vector", "FromA", EPropertyType.IntProperty),
            ("/Game/X.Vector", "FromX", EPropertyType.IntProperty))).Mappings);

        using var archive = BuildUnversionedArchive(mappings, writer => writer.Write(55));
        Assert.Throws<ParserException>(() => new FScriptStruct(archive, "Vector", null, ReadType.NORMAL));
    }

    [Fact]
    public void StructFallbackConstructorEnforcesCompleteIdentifierResolution()
    {
        var uniqueMappings = Assert.IsType<TypeMappings>(new UsmapParser(BuildUsmap(
            ("/Game/A.UniquePayload", "Value", EPropertyType.IntProperty))).Mappings);
        using (var archive = BuildUnversionedArchive(uniqueMappings, writer => writer.Write(77)))
        {
            var fallback = new FStructFallback(archive, "UniquePayload");
            Assert.Equal("Value", Assert.Single(fallback.Properties).Name.Text);
        }

        var ambiguousMappings = Assert.IsType<TypeMappings>(new UsmapParser(BuildUsmap(
            ("/Game/A.Collision", "FromA", EPropertyType.IntProperty),
            ("/Game/X.Collision", "FromX", EPropertyType.IntProperty))).Mappings);
        using var ambiguousArchive = BuildUnversionedArchive(ambiguousMappings, writer => writer.Write(77));
        Assert.Throws<ParserException>(() => new FStructFallback(ambiguousArchive, "Collision"));
    }

    [Fact]
    public void AngelScriptStructNamedLikeBuiltInUsesItsExactSchema()
    {
        var parser = new UsmapParser(BuildUsmap(
            ("/Script/Angelscript.Vector", "ScriptValue", EPropertyType.IntProperty),
            ("/Script/CoreUObject.Vector", "X", EPropertyType.DoubleProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var archive = BuildUnversionedArchive(mappings, writer => writer.Write(42));
        var value = new FScriptStruct(archive, "/Script/Angelscript.Vector", null, ReadType.NORMAL);
        var fallback = Assert.IsType<FStructFallback>(value.StructType);
        var property = Assert.Single(fallback.Properties);
        Assert.Equal("ScriptValue", property.Name.Text);
        Assert.Equal(42, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Fact]
    public void NonAngelScriptStructNamedLikeBuiltInUsesItsExactSchema()
    {
        const string customIdentifier = "/Script/Custom.Vector";
        const string nativeIdentifier = "/Script/CoreUObject.Vector";
        var parser = new UsmapParser(BuildUsmap(
            (customIdentifier, "CustomValue", EPropertyType.IntProperty),
            (nativeIdentifier, "X", EPropertyType.FloatProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var customArchive = BuildUnversionedArchive(mappings, writer => writer.Write(73));
        var custom = new FScriptStruct(customArchive, customIdentifier, null, ReadType.NORMAL);
        var customProperty = Assert.Single(Assert.IsType<FStructFallback>(custom.StructType).Properties);
        Assert.Equal("CustomValue", customProperty.Name.Text);
        Assert.Equal(73, Assert.IsType<IntProperty>(customProperty.Tag).Value);

        using var nativeArchive = BuildArchive(mappings, _ => { });
        var native = new FScriptStruct(nativeArchive, nativeIdentifier, null, ReadType.ZERO);
        Assert.Equal("FVector", native.StructType.GetType().Name);
    }

    [Fact]
    public void ArcRaidersScriptCollisionUsesTheExactIdentifierOnBothSides()
    {
        const string scriptIdentifier = "/Script/Angelscript.AISensingStatusTransition";
        const string nativeIdentifier = "/Script/EmbarkAI.AISensingStatusTransition";
        var parser = new UsmapParser(BuildUsmap(
            (scriptIdentifier, "ScriptValue", EPropertyType.IntProperty),
            (nativeIdentifier, "NativeValue", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var scriptArchive = BuildArchive(mappings, writer =>
        {
            writer.Write((ushort) 0x0300);
            writer.Write(11);
        }, EGame.GAME_ArcRaiders);
        var script = new FScriptStruct(scriptArchive, scriptIdentifier, null, ReadType.NORMAL);
        Assert.Equal("ScriptValue", Assert.Single(Assert.IsType<FStructFallback>(script.StructType).Properties).Name.Text);

        using var nativeArchive = BuildArchive(mappings, writer =>
        {
            writer.Write((ushort) 0x0300);
            writer.Write(22);
        }, EGame.GAME_ArcRaiders);
        var native = new FScriptStruct(nativeArchive, nativeIdentifier, null, ReadType.NORMAL);
        Assert.Equal("NativeValue", Assert.Single(Assert.IsType<FStructFallback>(native.StructType).Properties).Name.Text);
    }

    [Fact]
    public void SyntheticImportedClassRetainsExactIdentifier()
    {
        var parser = new UsmapParser(BuildUsmap(
            ("/Game/A.Payload", "FromA", EPropertyType.IntProperty),
            ("/Game/X.Payload", "FromX", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);
        var importedClass = new UScriptClass("Payload", "/Game/X.Payload");

        using var archive = BuildUnversionedArchive(mappings, writer => writer.Write(77));
        var fallback = new FStructFallback(archive, importedClass);
        var property = Assert.Single(fallback.Properties);
        Assert.Equal("FromX", property.Name.Text);
        Assert.Equal(77, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Fact]
    public void StandardShortNameMappingRemainsCompatible()
    {
        var parser = new UsmapParser(BuildUsmap(
            ("B", "Legacy", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        Assert.False(mappings.UsesFullTypeIdentifiers);
        Assert.True(mappings.TryGetType("B", "/Game/A.B", out var schema));
        Assert.True(mappings.TryGetType("/Game/A.B", "/Game/A.B", out _));
        Assert.Equal("Legacy", Assert.Single(schema.Properties.Values).Name);
    }

    [Fact]
    public void StandardShortNameFallbackStillDeserializes()
    {
        var parser = new UsmapParser(BuildUsmap(
            ("LegacyPayload", "Value", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var archive = BuildUnversionedArchive(mappings, writer => writer.Write(1234));
        var value = new FScriptStruct(archive, "LegacyPayload", null, ReadType.NORMAL);
        var fallback = Assert.IsType<FStructFallback>(value.StructType);
        var property = Assert.Single(fallback.Properties);
        Assert.Equal("Value", property.Name.Text);
        Assert.Equal(1234, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Fact]
    public void FullIdentifierRawStructDoesNotReadAnUnversionedHeader()
    {
        const int marker = 0x55667788;
        var parser = new UsmapParser(BuildUsmap(
            ("/Game/A.Payload", "Value", EPropertyType.IntProperty),
            ("/Game/X.Payload", "Other", EPropertyType.FloatProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var archive = BuildArchive(mappings, writer =>
        {
            writer.Write(1234);
            writer.Write(marker);
        });
        var value = new FScriptStruct(archive, "/Game/A.Payload", null, ReadType.RAW);
        var fallback = Assert.IsType<FStructFallback>(value.StructType);
        var property = Assert.Single(fallback.Properties);
        Assert.Equal("Value", property.Name.Text);
        Assert.Equal(1234, Assert.IsType<IntProperty>(property.Tag).Value);
        Assert.Equal(marker, archive.Read<int>());
    }

    [Fact]
    public void MovieSceneSerializedTypePathSelectsExactSchema()
    {
        const string typeIdentifier = "/Game/X.Payload";
        var parser = new UsmapParser(BuildUsmap(
            ("/Game/A.Payload", "FromA", EPropertyType.IntProperty),
            (typeIdentifier, "FromX", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var archive = BuildArchive(mappings, writer =>
        {
            WriteFString(writer, typeIdentifier);
            writer.Write((ushort) 0x0300); // one non-zero property, final fragment
            writer.Write(77);
        });
        var value = new FMovieSceneTrackImplementationPtr(archive);
        Assert.Equal(typeIdentifier, value.TypeName);
        var property = Assert.Single(Assert.IsType<FStructFallback>(value.Data).Properties);
        Assert.Equal("FromX", property.Name.Text);
        Assert.Equal(77, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Fact]
    public void MovieSceneSpecialSerializerRequiresItsCanonicalIdentifier()
    {
        const string typeIdentifier = "/Game/X.MovieSceneLiveLinkSectionTemplate";
        var parser = new UsmapParser(BuildUsmap(
            (typeIdentifier, "MappedValue", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var archive = BuildArchive(mappings, writer =>
        {
            WriteFString(writer, typeIdentifier);
            writer.Write((ushort) 0x0300);
            writer.Write(89);
        });
        var value = new FMovieSceneEvalTemplatePtr(archive);
        var property = Assert.Single(Assert.IsType<FStructFallback>(value.Data).Properties);
        Assert.Equal("MappedValue", property.Name.Text);
        Assert.Equal(89, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Theory]
    [InlineData(EGame.GAME_TitanQuest2)]
    [InlineData(EGame.GAME_DuneAwakening)]
    [InlineData(EGame.GAME_MortalKombat1)]
    [InlineData(EGame.GAME_Borderlands4)]
    public void GameSpecificFallbacksRetainExactIdentifier(EGame game)
    {
        const string typeIdentifier = "/Script/B.Payload";
        var parser = new UsmapParser(BuildUsmap(
            ("/Script/A.Payload", "FromA", EPropertyType.IntProperty),
            (typeIdentifier, "FromB", EPropertyType.IntProperty)));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        using var archive = BuildArchive(mappings, writer =>
        {
            writer.Write((ushort) 0x0300); // one non-zero property, final fragment
            writer.Write(91);
        }, game);
        var value = new FScriptStruct(archive, typeIdentifier, null, ReadType.NORMAL);
        var property = Assert.Single(Assert.IsType<FStructFallback>(value.StructType).Properties);
        Assert.Equal("FromB", property.Name.Text);
        Assert.Equal(91, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Fact]
    public void BlueprintCppFormattingUsesLeafOnlyAtOutputBoundary()
    {
        var structTag = new FPropertyTagData
        {
            Type = "StructProperty",
            StructType = "/Game/A.Payload"
        };
        var enumTag = new FPropertyTagData
        {
            Type = "EnumProperty",
            EnumName = "/Script/Example.EState"
        };

        Assert.Equal("FPayload", BlueprintDecompilerUtils.GetPropertyTagCppType(structTag));
        Assert.Equal("EState", BlueprintDecompilerUtils.GetPropertyTagCppType(enumTag));

        var enumProperty = new FPropertyTag(
            new FName("EnumProperty"),
            new EnumProperty(new FName("/Script/Example.EState::Ready")),
            enumTag);
        Assert.True(BlueprintDecompilerUtils.GetPropertyTagVariable(enumProperty, out var enumType, out var enumValue));
        Assert.Equal("enum EState", enumType);
        Assert.Equal("EState::Ready", enumValue);

        var byteProperty = new FPropertyTag(
            new FName("ByteProperty"),
            new EnumProperty(new FName("/Script/Example.EState::Stopped")),
            enumTag);
        Assert.True(BlueprintDecompilerUtils.GetPropertyTagVariable(byteProperty, out var byteType, out var byteValue));
        Assert.Equal("enum EState", byteType);
        Assert.Equal("EState::Stopped", byteValue);
    }

    [Fact]
    public void MixedFullAndShortIdentifiersAreRejected()
    {
        var data = BuildUsmap(
            ("/Game/A.B", "FromA", EPropertyType.IntProperty),
            ("B", "Ambiguous", EPropertyType.FloatProperty));

        var error = Assert.ThrowsAny<Exception>(() => new UsmapParser(data));
        Assert.Contains("mixes full and short", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateFullIdentifierIsRejectedInsteadOfLastWins()
    {
        var data = BuildUsmap(
            ("/Game/A.B", "First", EPropertyType.IntProperty),
            ("/Game/A.B", "Second", EPropertyType.FloatProperty));

        var error = Assert.ThrowsAny<Exception>(() => new UsmapParser(data));
        Assert.Contains("duplicate full type identifier", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingNestedFullIdentifierIsRejected()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        var nestedMissingStruct = new PropertyType(
            "ArrayProperty",
            innerType: new PropertyType("StructProperty", structType: "/Game/Missing.Payload"));
        mappings.Types["/Game/A.Owner"] = new Struct(
            mappings,
            "/Game/A.Owner",
            null,
            new Dictionary<int, PropertyInfo>
            {
                [0] = new PropertyInfo(0, "Items", nestedMissingStruct, 1)
            },
            1);

        var error = Assert.Throws<ArgumentException>(mappings.ValidateIdentifierMode);
        Assert.Contains("missing struct /Game/Missing.Payload", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NestedStructReferenceRetainsExactIdentifier()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types["/Game/A.Payload"] = SinglePropertyStruct(
            mappings, "/Game/A.Payload", "FromA", EPropertyType.FloatProperty);
        mappings.Types["/Game/X.Payload"] = SinglePropertyStruct(
            mappings, "/Game/X.Payload", "FromX", EPropertyType.IntProperty);
        mappings.Types["/Game/Owner.Container"] = new Struct(
            mappings,
            "/Game/Owner.Container",
            null,
            new Dictionary<int, PropertyInfo>
            {
                [0] = new PropertyInfo(0, "Nested",
                    new PropertyType("StructProperty", structType: "/Game/X.Payload"), 1)
            },
            1);
        mappings.ValidateIdentifierMode();

        using var archive = BuildArchive(mappings, writer =>
        {
            writer.Write((ushort) 0x0300); // owner property
            writer.Write((ushort) 0x0300); // nested property
            writer.Write(2468);
        });
        var owner = new FStructFallback(archive, "/Game/Owner.Container");
        var nestedProperty = Assert.IsType<StructProperty>(Assert.Single(owner.Properties).Tag);
        var nested = Assert.IsType<FStructFallback>(nestedProperty.Value!.StructType);
        var property = Assert.Single(nested.Properties);
        Assert.Equal("FromX", property.Name.Text);
        Assert.Equal(2468, Assert.IsType<IntProperty>(property.Tag).Value);
    }

    [Fact]
    public void FullIdentifierInheritanceCycleIsRejected()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types["/Game/A.First"] = EmptyStruct(mappings, "/Game/A.First", "/Game/B.Second");
        mappings.Types["/Game/B.Second"] = EmptyStruct(mappings, "/Game/B.Second", "/Game/A.First");

        var error = Assert.Throws<ArgumentException>(mappings.ValidateIdentifierMode);
        Assert.Contains("inheritance cycle", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SameLeafEnumResolvesByFullObjectIdentifier()
    {
        var mappings = new TypeMappings([], new Dictionary<string, Dictionary<long, string>>
        {
            ["/Game/A.EState"] = new Dictionary<long, string> { [0] = "FromA" },
            ["/Game/X.EState"] = new Dictionary<long, string> { [0] = "FromX" }
        });
        mappings.ValidateIdentifierMode();

        Assert.True(mappings.TryGetEnum("EState", "/Game/A.EState", out var fromA));
        Assert.True(mappings.TryGetEnum("EState", "/Game/X.EState", out var fromX));
        Assert.False(mappings.TryGetEnum("EState", null, out _));
        Assert.Equal("FromA", fromA[0]);
        Assert.Equal("FromX", fromX[0]);
    }

    [Fact]
    public void EnumPropertyConstructorEnforcesCompleteIdentifierResolution()
    {
        var uniqueMappings = new TypeMappings([], new Dictionary<string, Dictionary<long, string>>
        {
            ["/Game/A.EState"] = new Dictionary<long, string> { [0] = "FromA" }
        });
        using (var archive = BuildArchive(uniqueMappings, writer => writer.Write((byte) 0)))
        {
            var value = new EnumProperty(archive, new FPropertyTagData
            {
                Type = "EnumProperty",
                EnumName = "EState"
            }, ReadType.NORMAL);
            Assert.Equal("/Game/A.EState::FromA", value.Value.Text);
        }

        var ambiguousMappings = new TypeMappings([], new Dictionary<string, Dictionary<long, string>>
        {
            ["/Game/A.EState"] = new Dictionary<long, string> { [0] = "FromA" },
            ["/Game/X.EState"] = new Dictionary<long, string> { [0] = "FromX" }
        });
        using var ambiguousArchive = BuildArchive(ambiguousMappings, writer => writer.Write((byte) 0));
        var tagData = new FPropertyTagData { Type = "EnumProperty", EnumName = "EState" };
        Assert.Throws<ParserException>(() => new EnumProperty(ambiguousArchive, tagData, ReadType.NORMAL));
    }

    [Fact]
    public void CompletePropertyTypeModuleReconstructsExactIdentifiers()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var enums = new Dictionary<string, Dictionary<long, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["/Script/A.EState"] = new Dictionary<long, string> { [0] = "FromA" },
            ["/Script/X.EState"] = new Dictionary<long, string> { [0] = "FromX" }
        };
        var mappings = new TypeMappings(types, enums);
        mappings.Types["/Game/A.Payload"] = SinglePropertyStruct(
            mappings, "/Game/A.Payload", "FromA", EPropertyType.IntProperty);
        mappings.Types["/Game/X.Payload"] = SinglePropertyStruct(
            mappings, "/Game/X.Payload", "FromX", EPropertyType.IntProperty);
        mappings.ValidateIdentifierMode();

        var structTag = new FPropertyTagData
        {
            Type = "StructProperty",
            StructType = "Payload",
            Module = "/Game/X"
        };
        Assert.Equal("/Game/X.Payload", structTag.GetStructTypeIdentifier());
        using var structArchive = BuildUnversionedArchive(mappings, writer => writer.Write(101));
        var structProperty = new StructProperty(structArchive, structTag, ReadType.NORMAL);
        var nested = Assert.IsType<FStructFallback>(structProperty.Value!.StructType);
        Assert.Equal("FromX", Assert.Single(nested.Properties).Name.Text);

        var enumTag = new FPropertyTagData
        {
            Type = "EnumProperty",
            EnumName = "EState",
            Module = "X"
        };
        Assert.Equal("/Script/X.EState", enumTag.GetEnumTypeIdentifier());
        using var enumArchive = BuildArchive(mappings, writer => writer.Write((byte) 0));
        var enumProperty = new EnumProperty(enumArchive, enumTag, ReadType.RAW);
        Assert.Equal("/Script/X.EState::FromX", enumProperty.Value.Text);
    }

    [Fact]
    public void TaggedPropertyUsesOwnerSchemaForNestedCompleteIdentifiers()
    {
        var mappings = new TypeMappings();
        mappings.Types["/Game/A.Payload"] = EmptyStruct(mappings, "/Game/A.Payload");
        mappings.Types["/Game/X.Payload"] = EmptyStruct(mappings, "/Game/X.Payload");
        mappings.Enums["/Game/A.EState"] = new Dictionary<long, string> { [0] = "FromA" };
        mappings.Enums["/Game/X.EState"] = new Dictionary<long, string> { [0] = "FromX" };
        var mapType = new PropertyType("MapProperty",
            innerType: new PropertyType("StructProperty", structType: "/Game/X.Payload"),
            valueType: new PropertyType("EnumProperty",
                innerType: new PropertyType("ByteProperty"), enumName: "/Game/X.EState"));
        var owner = new Struct(mappings, "/Game/Owner.Container", null,
            new Dictionary<int, PropertyInfo>
            {
                [0] = new PropertyInfo(0, "Items", mapType, 1)
            }, 1);
        mappings.Types[owner.Name] = owner;
        mappings.ValidateIdentifierMode();

        using var archive = BuildArchive(mappings, writer =>
        {
            WriteFName(writer, 1); // Items
            WriteFName(writer, 2); // MapProperty
            writer.Write(0); // serialized value size
            writer.Write(0); // array index
            WriteFName(writer, 3); // StructProperty key
            WriteFName(writer, 4); // EnumProperty value
            writer.Write((byte) 0); // no property GUID
        }, EGame.GAME_UE5_3, unversioned: false,
            names: ["None", "Items", "MapProperty", "StructProperty", "EnumProperty"]);

        var tag = new FPropertyTag(archive, false, owner);

        Assert.Equal("/Game/X.Payload", tag.TagData?.InnerTypeData?.GetStructTypeIdentifier());
        Assert.Equal("/Game/X.EState", tag.TagData?.ValueTypeData?.GetEnumTypeIdentifier());
    }

    [Fact]
    public void MappingIndexesAreRebuiltAfterDictionaryMutation()
    {
        var types = new Dictionary<string, Struct>(StringComparer.Ordinal);
        var mappings = new TypeMappings(types, []);
        mappings.Types["/Game/A.Payload"] = SinglePropertyStruct(
            mappings, "/Game/A.Payload", "FromA", EPropertyType.IntProperty);

        Assert.True(mappings.TryGetType("Payload", "/Game/A.Payload", out _));
        Assert.True(mappings.IsTypeIdentifierLeafUnique("/Game/A.Payload"));

        mappings.Types["/Game/X.Payload"] = SinglePropertyStruct(
            mappings, "/Game/X.Payload", "FromX", EPropertyType.IntProperty);
        Assert.False(mappings.TryGetType("Payload", null, out _));
        Assert.False(mappings.IsTypeIdentifierLeafUnique("/Game/A.Payload"));
        Assert.True(mappings.TryGetType("Payload", "/Game/X.Payload", out var fromX));
        Assert.Equal("FromX", Assert.Single(fromX.Properties.Values).Name);

        mappings.Types["/Game/X.Payload"] = SinglePropertyStruct(
            mappings, "/Game/X.Payload", "Replacement", EPropertyType.FloatProperty);
        Assert.True(mappings.TryGetType("Payload", "/game/x.payload", out var replacement));
        Assert.Equal("Replacement", Assert.Single(replacement.Properties.Values).Name);
    }

    [Fact]
    public void MappingNestedMutationForcesReferenceRevalidation()
    {
        var mappings = new TypeMappings();
        var nested = new PropertyType("StructProperty", structType: "/Game/A.Payload");
        mappings.Types["/Game/A.Payload"] = EmptyStruct(mappings, "/Game/A.Payload");
        mappings.Types["/Game/A.Owner"] = new Struct(mappings, "/Game/A.Owner", null,
            new Dictionary<int, PropertyInfo>
            {
                [0] = new PropertyInfo(0, "Nested", nested, 1)
            }, 1);
        mappings.ValidateIdentifierMode();

        nested.StructType = "/Game/Missing.Payload";

        var error = Assert.Throws<ArgumentException>(() => _ = mappings.UsesFullTypeIdentifiers);
        Assert.Contains("missing struct /Game/Missing.Payload", error.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TypeMappingsSnapshotsConstructorDictionaries()
    {
        var source = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(source, []);
        source["/Game/A.Payload"] = EmptyStruct(mappings, "/Game/A.Payload");

        Assert.False(mappings.UsesFullTypeIdentifiers);
        Assert.Empty(mappings.Types);
    }

    [Fact]
    public void MappingMutationResetsPreviouslyResolvedSuper()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        var child = EmptyStruct(mappings, "Child", "Parent");
        mappings.Types[child.Name] = child;

        Assert.True(mappings.TryGetType("Child", null, out _));
        Assert.Null(child.Super.Value);

        var parent = EmptyStruct(mappings, "Parent");
        mappings.Types[parent.Name] = parent;
        Assert.True(mappings.TryGetType("Child", null, out _));
        Assert.Same(parent, child.Super.Value);
    }

    [Fact]
    public void RemovingMappingsInvalidatesUniqueIdentifierIndexes()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types["/Game/A.Payload"] = EmptyStruct(mappings, "/Game/A.Payload");

        Assert.True(mappings.TryResolveUniqueTypeIdentifier("Payload", out _));
        mappings.Types.Clear();
        Assert.False(mappings.TryResolveUniqueTypeIdentifier("Payload", out _));
        Assert.False(mappings.UsesFullTypeIdentifiers);
    }

    [Fact]
    public void FullStructIdentifierNeverFallsBackToNativeLeafDispatch()
    {
        using var archive = BuildArchive(new TypeMappings(), _ => { });
        var value = new FScriptStruct(archive, "/Game/X.Vector", null, ReadType.ZERO);
        Assert.IsType<FStructFallback>(value.StructType);
    }

    [Fact]
    public void FullIdentifierRetainsBorderlandsCustomSerializer()
    {
        const string identifier = "/Script/Test.GbxInlineStruct";
        var mappings = new TypeMappings();
        mappings.Types[identifier] = EmptyStruct(mappings, identifier);
        using var archive = BuildArchive(mappings, writer => writer.Write(0), EGame.GAME_Borderlands4);

        var value = new FScriptStruct(archive, identifier, null, ReadType.NORMAL);

        Assert.IsType<FGbxInlineStruct>(value.StructType);
    }

    [Fact]
    public void ObjectRegistryRequiresExactRegistrationForDuplicateLeaves()
    {
        const string fromA = "/Script/A.LongIdentifierRegistryProbe";
        const string fromX = "/Script/X.LongIdentifierRegistryProbe";
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types[fromA] = EmptyStruct(mappings, fromA);
        mappings.Types[fromX] = EmptyStruct(mappings, fromX);
        mappings.ValidateIdentifierMode();

        ObjectTypeRegistry.RegisterClass("LongIdentifierRegistryProbe", typeof(LongIdentifierRegistryProbe));
        Assert.Null(new UScriptClass("LongIdentifierRegistryProbe", fromA)
            .ConstructObject(EObjectFlags.RF_NoFlags, fromA, mappings));
        Assert.Null(new UScriptClass("LongIdentifierRegistryProbe", fromX)
            .ConstructObject(EObjectFlags.RF_NoFlags, fromX, mappings));

        ObjectTypeRegistry.RegisterClass(fromA, typeof(LongIdentifierRegistryProbe));
        Assert.IsType<LongIdentifierRegistryProbe>(new UScriptClass("LongIdentifierRegistryProbe", fromA)
            .ConstructObject(EObjectFlags.RF_NoFlags, fromA, mappings));
        Assert.Null(new UScriptClass("LongIdentifierRegistryProbe", fromX)
            .ConstructObject(EObjectFlags.RF_NoFlags, fromX, mappings));
        Assert.Throws<ArgumentException>(() =>
            ObjectTypeRegistry.RegisterClass(fromA, typeof(SecondLongIdentifierRegistryProbe)));
    }

    [Fact]
    public void ObjectRegistryUsesDeclaredFullIdentifierForGameParser()
    {
        const string identifier = "/Script/CommonUI.CommonGenericInputActionDataTable";
        var mappings = new TypeMappings();
        mappings.Types[identifier] = EmptyStruct(mappings, identifier);

        var value = new UScriptClass("CommonGenericInputActionDataTable", identifier)
            .ConstructObject(EObjectFlags.RF_NoFlags, identifier, mappings);

        Assert.IsType<UCommonGenericInputActionDataTable>(value);
    }

    [Fact]
    public void ObjectRegistryDoesNotUseUniqueLeafAsFullRegistration()
    {
        const string identifier = "/Script/A.LongIdentifierRegistryProbe";
        var mappings = new TypeMappings();
        mappings.Types[identifier] = EmptyStruct(mappings, identifier);
        ObjectTypeRegistry.RegisterClass("LongIdentifierRegistryProbe", typeof(LongIdentifierRegistryProbe));

        Assert.Null(new UScriptClass("LongIdentifierRegistryProbe", identifier)
            .ConstructObject(EObjectFlags.RF_NoFlags, identifier, mappings));
    }

    [Fact]
    public void ObjectRegistryDoesNotGuessGeneratedClassBaseFromSuffix()
    {
        const string generated = "/Game/A.LongIdentifierRegistryProbe_C";
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types[generated] = EmptyStruct(mappings, generated);
        mappings.ValidateIdentifierMode();

        ObjectTypeRegistry.RegisterClass("LongIdentifierRegistryProbe", typeof(LongIdentifierRegistryProbe));
        Assert.Null(new UScriptClass("LongIdentifierRegistryProbe_C", generated)
            .ConstructObject(EObjectFlags.RF_NoFlags, generated, mappings));
    }

    [Fact]
    public void LongMappingRejectsAmbiguousShortObjectParserRegistrations()
    {
        const string identifier = "/Script/A.AmbiguousRegistryProbe";
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types[identifier] = EmptyStruct(mappings, identifier);
        mappings.ValidateIdentifierMode();

        ObjectTypeRegistry.RegisterClass("AmbiguousRegistryProbe", typeof(LongIdentifierRegistryProbe));
        ObjectTypeRegistry.RegisterClass("AmbiguousRegistryProbe", typeof(SecondLongIdentifierRegistryProbe));
        Assert.Null(new UScriptClass("AmbiguousRegistryProbe", identifier)
            .ConstructObject(EObjectFlags.RF_NoFlags, identifier, mappings));
    }

    [Fact]
    public void LongMappingRejectsAmbiguousLeafOnlyObjectIdentity()
    {
        const string fromA = "/Script/A.LeafOnlyRegistryProbe";
        const string fromX = "/Script/X.LeafOnlyRegistryProbe";
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types[fromA] = EmptyStruct(mappings, fromA);
        mappings.Types[fromX] = EmptyStruct(mappings, fromX);
        mappings.ValidateIdentifierMode();

        ObjectTypeRegistry.RegisterClass("LeafOnlyRegistryProbe", typeof(LongIdentifierRegistryProbe));
        Assert.Null(ObjectTypeRegistry.GetClass("LeafOnlyRegistryProbe", mappings: mappings));
    }

    [Fact]
    public void LegacyObjectRegistryUsesFNameCaseSemantics()
    {
        ObjectTypeRegistry.RegisterClass("CaseInsensitiveRegistryProbe", typeof(LongIdentifierRegistryProbe));

        Assert.Equal(typeof(LongIdentifierRegistryProbe),
            ObjectTypeRegistry.GetClass("caseinsensitiveregistryprobe"));
    }

    [Fact]
    public void BlueprintPrefixLookupKeepsDuplicateClassIdentifiersSeparate()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        mappings.Types["/Script/CoreUObject.Object"] = EmptyStruct(mappings, "/Script/CoreUObject.Object");
        mappings.Types["/Script/Engine.Actor"] = EmptyStruct(
            mappings, "/Script/Engine.Actor", "/Script/CoreUObject.Object");
        mappings.Types["/Script/A.Duplicate"] = EmptyStruct(
            mappings, "/Script/A.Duplicate", "/Script/Engine.Actor");
        mappings.Types["/Script/X.Duplicate"] = EmptyStruct(
            mappings, "/Script/X.Duplicate", "/Script/CoreUObject.Object");
        mappings.Types["/Script/Custom.Actor"] = EmptyStruct(
            mappings, "/Script/Custom.Actor", "/Script/CoreUObject.Object");
        mappings.Types["/Script/Custom.Interface"] = EmptyStruct(
            mappings, "/Script/Custom.Interface", "/Script/Engine.Actor");
        mappings.Types["/Script/Custom.Object"] = EmptyStruct(
            mappings, "/Script/Custom.Object", "/Script/Engine.Actor");
        mappings.ValidateIdentifierMode();

        var previous = BlueprintDecompilerUtils.Mappings;
        try
        {
            BlueprintDecompilerUtils.Mappings = mappings;
            Assert.Equal("A", BlueprintDecompilerUtils.GetClassPrefix("/Script/A.Duplicate"));
            Assert.Equal("U", BlueprintDecompilerUtils.GetClassPrefix("/Script/X.Duplicate"));
            Assert.Equal("U", BlueprintDecompilerUtils.GetClassPrefix("/Script/Custom.Actor"));
            Assert.Equal("A", BlueprintDecompilerUtils.GetClassPrefix("/Script/Custom.Interface"));
            Assert.Equal("A", BlueprintDecompilerUtils.GetClassPrefix("/Script/Custom.Object"));
            Assert.Equal("U", BlueprintDecompilerUtils.GetClassPrefix("Actor"));
        }
        finally
        {
            BlueprintDecompilerUtils.Mappings = previous;
        }
    }

    [Fact]
    public void SameFullIdentifierCannotDescribeBothATypeAndAnEnum()
    {
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var enums = new Dictionary<string, Dictionary<long, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["/Game/A.Collision"] = new Dictionary<long, string> { [0] = "Value" }
        };
        var mappings = new TypeMappings(types, enums);
        mappings.Types["/Game/A.Collision"] = EmptyStruct(mappings, "/Game/A.Collision");

        var error = Assert.Throws<ArgumentException>(mappings.ValidateIdentifierMode);
        Assert.Contains("both a type and an enum", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullEnumIdentifiersUseFNameCaseSemantics()
    {
        var parser = new UsmapParser(BuildEnumUsmap(
            ("/Game/A.EState", "Ready")));
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        Assert.True(mappings.TryGetEnum("estate", "/game/a.estate", out var values));
        Assert.Equal("Ready", values[0]);
    }

    [Fact]
    public void FullIdentifierLookupUsesFNameCaseSemanticsWithOrdinalStorage()
    {
        var parser = new UsmapParser(
            BuildUsmap(("/Game/A.Payload", "Value", EPropertyType.IntProperty)),
            comparer: StringComparer.Ordinal);
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        Assert.True(mappings.TryGetType("payload", "/game/a.payload", out var value));
        Assert.Equal("Value", Assert.Single(value.Properties.Values).Name);
    }

    [Fact]
    public void CaseOnlyDuplicateFullTypeIsRejectedWithOrdinalStorage()
    {
        var data = BuildUsmap(
            ("/Game/A.Payload", "First", EPropertyType.IntProperty),
            ("/game/a.payload", "Second", EPropertyType.IntProperty));

        var error = Assert.ThrowsAny<Exception>(() =>
            new UsmapParser(data, comparer: StringComparer.Ordinal));
        Assert.Contains("FName case semantics", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CaseOnlyDuplicateFullEnumIdentifierIsRejected()
    {
        var data = BuildEnumUsmap(
            ("/Game/A.EState", "Ready"),
            ("/game/a.estate", "Stopped"));

        var error = Assert.ThrowsAny<Exception>(() => new UsmapParser(data));
        Assert.Contains("duplicate full enum identifier", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeepAcyclicInheritanceUsesBoundedCallStack()
    {
        const int count = 20_000;
        var types = new Dictionary<string, Struct>(StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(types, []);
        string? super = null;
        for (var index = 0; index < count; index++)
        {
            var name = $"/Game/Deep.Type{index}";
            mappings.Types[name] = EmptyStruct(mappings, name, super);
            super = name;
        }

        mappings.ValidateIdentifierMode();
        Assert.True(mappings.UsesFullTypeIdentifiers);
    }

    private static Struct EmptyStruct(TypeMappings mappings, string name, string? super = null) =>
        new(mappings, name, super, [], 0);

    private static Struct SinglePropertyStruct(TypeMappings mappings, string name, string property,
        EPropertyType kind) =>
        new(mappings, name, null, new Dictionary<int, PropertyInfo>
        {
            [0] = new PropertyInfo(0, property, new PropertyType(kind.ToString()), 1)
        }, 1);

    private static byte[] BuildUsmap(params (string Type, string Property, EPropertyType Kind)[] schemas)
    {
        var names = schemas
            .SelectMany(x => new[] { x.Type, x.Property })
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var nameIndices = names
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        using var payloadStream = new MemoryStream();
        using (var payload = new BinaryWriter(payloadStream, Encoding.UTF8, leaveOpen: true))
        {
            payload.Write((uint) names.Length);
            foreach (var name in names)
            {
                var bytes = Encoding.UTF8.GetBytes(name);
                payload.Write((ushort) bytes.Length);
                payload.Write(bytes);
            }

            payload.Write(0u); // enums
            payload.Write((uint) schemas.Length);
            foreach (var schema in schemas)
            {
                payload.Write(nameIndices[schema.Type]);
                payload.Write(-1); // no super
                payload.Write((ushort) 1); // total properties
                payload.Write((ushort) 1); // serialized properties
                payload.Write((ushort) 0); // schema index
                payload.Write((byte) 1); // array dimension
                payload.Write(nameIndices[schema.Property]);
                payload.Write((byte) schema.Kind);
            }
        }

        var payloadBytes = payloadStream.ToArray();
        using var fileStream = new MemoryStream();
        using (var file = new BinaryWriter(fileStream, Encoding.UTF8, leaveOpen: true))
        {
            file.Write((ushort) 0x30C4);
            file.Write((byte) EUsmapVersion.LongFName);
            file.Write(0); // no package versioning
            file.Write((byte) EUsmapCompressionMethod.None);
            file.Write((uint) payloadBytes.Length);
            file.Write((uint) payloadBytes.Length);
            file.Write(payloadBytes);
        }

        return fileStream.ToArray();
    }

    private static byte[] BuildEnumUsmap(params (string Enum, string Member)[] enums)
    {
        var names = enums
            .SelectMany(x => new[] { x.Enum, x.Member })
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var nameIndices = names
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        using var payloadStream = new MemoryStream();
        using (var payload = new BinaryWriter(payloadStream, Encoding.UTF8, leaveOpen: true))
        {
            payload.Write((uint) names.Length);
            foreach (var name in names)
            {
                var bytes = Encoding.UTF8.GetBytes(name);
                payload.Write((ushort) bytes.Length);
                payload.Write(bytes);
            }

            payload.Write((uint) enums.Length);
            foreach (var item in enums)
            {
                payload.Write(nameIndices[item.Enum]);
                payload.Write((byte) 1);
                payload.Write(nameIndices[item.Member]);
            }
            payload.Write(0u); // structs
        }

        return WrapUsmapPayload(payloadStream.ToArray());
    }

    private static FAssetArchive BuildUnversionedArchive(TypeMappings mappings, Action<BinaryWriter> writeValue)
    {
        return BuildArchive(mappings, writer =>
        {
            writer.Write((ushort) 0x0300); // one non-zero property, final fragment
            writeValue(writer);
        });
    }

    private static FAssetArchive BuildArchive(TypeMappings mappings, Action<BinaryWriter> write,
        EGame game = EGame.GAME_UE4_LATEST, bool unversioned = true, string[]? names = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            write(writer);
        var package = new MappingTestPackage(mappings, unversioned, names);
        return new FAssetArchive(
            new FByteArchive("long-identifier-test", stream.ToArray(), new VersionContainer(game)), package);
    }

    private static void WriteFString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        writer.Write(bytes.Length + 1);
        writer.Write(bytes);
        writer.Write((byte) 0);
    }

    private static void WriteFName(BinaryWriter writer, int nameIndex)
    {
        writer.Write(nameIndex);
        writer.Write(0);
    }

    private static byte[] WrapUsmapPayload(byte[] payloadBytes)
    {
        using var fileStream = new MemoryStream();
        using (var file = new BinaryWriter(fileStream, Encoding.UTF8, leaveOpen: true))
        {
            file.Write((ushort) 0x30C4);
            file.Write((byte) EUsmapVersion.LongFName);
            file.Write(0);
            file.Write((byte) EUsmapCompressionMethod.None);
            file.Write((uint) payloadBytes.Length);
            file.Write((uint) payloadBytes.Length);
            file.Write(payloadBytes);
        }
        return fileStream.ToArray();
    }

    private sealed class MappingTestPackage(TypeMappings mappings, bool unversioned = true, string[]? names = null) : IPackage
    {
        public string Name { get; set; } = "LongIdentifierTest";
        public IFileProvider? Provider => null;
        public TypeMappings? Mappings => mappings;
        public FPackageFileSummary Summary { get; } = null!;
        public FNameEntrySerialized[] NameMap { get; } =
            names?.Select(name => new FNameEntrySerialized(name)).ToArray() ?? [];
        public int ImportMapLength => 0;
        public int ExportMapLength => 0;
        public Lazy<UObject>[] ExportsLazy { get; } = [];
        public bool IsFullyLoaded => true;
        public bool CanDeserialize => true;

        public bool HasFlags(EPackageFlags flags)
        {
            var packageFlags = EPackageFlags.PKG_Cooked;
            if (unversioned)
                packageFlags |= EPackageFlags.PKG_UnversionedProperties;
            return packageFlags.HasFlag(flags);
        }

        public int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal) => -1;
        public ResolvedObject? ResolvePackageIndex(FPackageIndex? index) => null;
    }

    public sealed class LongIdentifierRegistryProbe : UObject;
    public sealed class SecondLongIdentifierRegistryProbe : UObject;

    private sealed class MappingJsonProvider : JsonTypeMappingsProvider
    {
        public bool AddTypes(string json) => AddStructs(json);
        public void AddEnumValues(string json) => AddEnums(json);
        public override void Load(string path, StringComparer? comparer = null) => throw new NotSupportedException();
        public override void Load(byte[] bytes, StringComparer? comparer = null) => throw new NotSupportedException();
        public override void Reload() => throw new NotSupportedException();
    }
}
