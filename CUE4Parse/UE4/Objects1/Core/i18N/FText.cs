using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Core.i18N
{
    public enum ETextHistoryType : sbyte
    {
        None = -1,
        Base = 0,
        NamedFormat,
        OrderedFormat,
        ArgumentFormat,
        AsNumber,
        AsPercent,
        AsCurrency,
        AsDate,
        AsTime,
        AsDateTime,
        Transform,
        StringTableEntry,
        TextGenerator,

        // Add new enum types at the end only! They are serialized by index.
    }

    public enum EFormatArgumentType : sbyte
    {
        Int,
        UInt,
        Float,
        Double,
        Text,
        Gender,

        // Add new enum types at the end only! They are serialized by index.
    }

    public enum ERoundingMode : sbyte
    {
        /** Rounds to the nearest place, equidistant ties go to the value which is closest to an even value: 1.5 becomes 2, 0.5 becomes 0 */
        HalfToEven,

        /** Rounds to nearest place, equidistant ties go to the value which is further from zero: -0.5 becomes -1.0, 0.5 becomes 1.0 */
        HalfFromZero,

        /** Rounds to nearest place, equidistant ties go to the value which is closer to zero: -0.5 becomes 0, 0.5 becomes 0. */
        HalfToZero,

        /** Rounds to the value which is further from zero, "larger" in absolute value: 0.1 becomes 1, -0.1 becomes -1 */
        FromZero,

        /** Rounds to the value which is closer to zero, "smaller" in absolute value: 0.1 becomes 0, -0.1 becomes 0 */
        ToZero,

        /** Rounds to the value which is more negative: 0.1 becomes 0, -0.1 becomes -1 */
        ToNegativeInfinity,

        /** Rounds to the value which is more positive: 0.1 becomes 1, -0.1 becomes 0 */
        ToPositiveInfinity,


        // Add new enum types at the end only! They are serialized by index.
    }

    public enum EDateTimeStyle : sbyte
    {
        Default,
        Short,
        Medium,
        Long,

        Full
        // Add new enum types at the end only! They are serialized by index.
    }

    public enum ETransformType : byte
    {
        ToLower = 0,
        ToUpper,

        // Add new enum types at the end only! They are serialized by index.
    }

    public enum EStringTableLoadingPhase : byte
    {
        /** This string table is pending load, and load should be attempted when possible */
        PendingLoad,

        /** This string table is currently being loaded, potentially asynchronously */
        Loading,

        /** This string was loaded, though that load may have failed */
        Loaded,
    }

    [JsonConverter(typeof(FTextConverter))]
    public class FText : IUStruct
    {
        public readonly uint Flags;
        public readonly ETextHistoryType HistoryType;
        public readonly FTextHistory TextHistory;
        public string Text => TextHistory.Text;

        public FText(FAssetArchive Ar)
        {
            Flags = Ar.Read<uint>();

            HistoryType = Ar.Read<ETextHistoryType>();
            TextHistory = HistoryType switch
            {
                ETextHistoryType.Base => new FTextHistory.Base(Ar),
                ETextHistoryType.NamedFormat => new FTextHistory.NamedFormat(Ar),
                ETextHistoryType.OrderedFormat => new FTextHistory.OrderedFormat(Ar),
                ETextHistoryType.ArgumentFormat => new FTextHistory.ArgumentFormat(Ar),
                ETextHistoryType.AsNumber => new FTextHistory.FormatNumber(Ar, HistoryType),
                ETextHistoryType.AsPercent => new FTextHistory.FormatNumber(Ar, HistoryType),
                ETextHistoryType.AsCurrency => new FTextHistory.FormatNumber(Ar, HistoryType),
                ETextHistoryType.AsDate => new FTextHistory.AsDate(Ar),
                ETextHistoryType.AsTime => new FTextHistory.AsTime(Ar),
                ETextHistoryType.AsDateTime => new FTextHistory.AsDateTime(Ar),
                ETextHistoryType.Transform => new FTextHistory.Transform(Ar),
                ETextHistoryType.StringTableEntry => new FTextHistory.StringTableEntry(Ar),
                ETextHistoryType.TextGenerator => new FTextHistory.TextGenerator(Ar),
                _ => new FTextHistory.None(Ar)
            };
        }

        public FText(string sourceString, string localizedString = "") : this("", "", sourceString, localizedString) { }

        public FText(string @namespace, string key, string sourceString, string localizedString = "") : this(0, ETextHistoryType.Base,
            new FTextHistory.Base(@namespace, key, sourceString, localizedString)) { }

        public FText(uint flags, ETextHistoryType historyType, FTextHistory textHistory)
        {
            Flags = flags;
            HistoryType = historyType;
            TextHistory = textHistory;
        }

        public override string ToString() => Text;
    }

    public class FTextConverter : JsonConverter<FText>
    {
        public override void WriteJson(JsonWriter writer, FText value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.TextHistory);
        }

        public override FText ReadJson(JsonReader reader, Type objectType, FText existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class FTextHistory : IUStruct
    {
        [JsonIgnore] public abstract string Text { get; }

        public class None : FTextHistory
        {
            public readonly string? CultureInvariantString;
            public override string Text => CultureInvariantString ?? string.Empty;

            public None()
            {

            }
            public None(FAssetArchive Ar)
            {
                if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.CultureInvariantTextSerializationKeyStability)
                {
                    var bHasCultureInvariantString = Ar.ReadBoolean();
                    if (bHasCultureInvariantString)
                    {
                        CultureInvariantString = Ar.ReadFString();
                    }
                }
            }
        }

        public class Base : FTextHistory
        {
            public readonly string Namespace;
            public readonly string Key;
            public readonly string SourceString;
            public readonly string LocalizedString;
            public override string Text => LocalizedString;

            public Base(FAssetArchive Ar)
            {
                Namespace = Ar.ReadFString() ?? string.Empty;
                Key = Ar.ReadFString() ?? string.Empty;
                SourceString = Ar.ReadFString();
                LocalizedString = Ar.Owner.Provider?.GetLocalizedString(Namespace, Key, SourceString) ?? string.Empty;
            }

            public Base(string namespacee, string key, string sourceString, string localizedString = "")
            {
                Namespace = namespacee;
                Key = key;
                SourceString = sourceString;
                LocalizedString = string.IsNullOrEmpty(localizedString) ? sourceString : localizedString;
            }
        }

        public class NamedFormat : FTextHistory
        {
            public readonly FText SourceFmt;

            public readonly Dictionary<string, FFormatArgumentValue>
                Arguments; /* called FFormatNamedArguments in UE4 */

            public override string Text => SourceFmt.Text;

            public NamedFormat(FAssetArchive Ar)
            {
                SourceFmt = new FText(Ar);
                Arguments = new Dictionary<string, FFormatArgumentValue>(Ar.Read<int>());
                for (int i = 0; i < Arguments.Count; i++)
                {
                    Arguments[Ar.ReadFString()] = new FFormatArgumentValue(Ar);
                }
            }
        }

        public class OrderedFormat : FTextHistory
        {
            public readonly FText SourceFmt;
            public readonly FFormatArgumentValue[] Arguments; /* called FFormatOrderedArguments in UE4 */
            public override string Text => SourceFmt.Text;

            public OrderedFormat(FAssetArchive Ar)
            {
                SourceFmt = new FText(Ar);
                Arguments = Ar.ReadArray(() => new FFormatArgumentValue(Ar));
            }
        }

        public class ArgumentFormat : FTextHistory
        {
            public readonly FText SourceFmt;
            public readonly FFormatArgumentData[] Arguments;
            public override string Text => SourceFmt.Text;

            public ArgumentFormat(FAssetArchive Ar)
            {
                SourceFmt = new FText(Ar);
                Arguments = Ar.ReadArray(() => new FFormatArgumentData(Ar));
            }
        }

        public class FormatNumber : FTextHistory
        {
            public readonly string? CurrencyCode;
            public readonly FFormatArgumentValue SourceValue;
            public readonly FNumberFormattingOptions? FormatOptions;
            public readonly string TargetCulture;
            public override string Text => SourceValue.Value.ToString();

            public FormatNumber(FAssetArchive Ar, ETextHistoryType historyType)
            {
                if (historyType == ETextHistoryType.AsCurrency &&
                    Ar.Ver >= EUnrealEngineObjectUE4Version.ADDED_CURRENCY_CODE_TO_FTEXT)
                {
                    CurrencyCode = Ar.ReadFString();
                }

                SourceValue = new FFormatArgumentValue(Ar);
                if (Ar.ReadBoolean()) // bHasFormatOptions
                {
                    FormatOptions = new FNumberFormattingOptions(Ar);
                }

                TargetCulture = Ar.ReadFString();
            }
        }

        public class AsDate : FTextHistory
        {
            public readonly FDateTime SourceDateTime;
            public readonly EDateTimeStyle DateStyle;
            public readonly string? TimeZone;
            public readonly string TargetCulture;
            public override string Text => SourceDateTime.ToString();

            public AsDate(FAssetArchive Ar)
            {
                SourceDateTime = Ar.Read<FDateTime>();
                DateStyle = Ar.Read<EDateTimeStyle>();
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.FTEXT_HISTORY_DATE_TIMEZONE)
                {
                    TimeZone = Ar.ReadFString();
                }

                TargetCulture = Ar.ReadFString();
            }
        }

        public class AsTime : FTextHistory
        {
            public readonly FDateTime SourceDateTime;
            public readonly EDateTimeStyle TimeStyle;
            public readonly string TimeZone;
            public readonly string TargetCulture;
            public override string Text => SourceDateTime.ToString();

            public AsTime(FAssetArchive Ar)
            {
                SourceDateTime = Ar.Read<FDateTime>();
                TimeStyle = Ar.Read<EDateTimeStyle>();
                TimeZone = Ar.ReadFString();
                TargetCulture = Ar.ReadFString();
            }
        }

        public class AsDateTime : FTextHistory
        {
            public readonly FDateTime SourceDateTime;
            public readonly EDateTimeStyle DateStyle;
            public readonly EDateTimeStyle TimeStyle;
            public readonly string TimeZone;
            public readonly string TargetCulture;
            public override string Text => SourceDateTime.ToString();

            public AsDateTime(FAssetArchive Ar)
            {
                SourceDateTime = Ar.Read<FDateTime>();
                DateStyle = Ar.Read<EDateTimeStyle>();
                TimeStyle = Ar.Read<EDateTimeStyle>();
                TimeZone = Ar.ReadFString();
                TargetCulture = Ar.ReadFString();
            }
        }

        public class Transform : FTextHistory
        {
            public readonly FText SourceText;
            public readonly ETransformType TransformType;
            public override string Text => SourceText.Text;

            public Transform(FAssetArchive Ar)
            {
                SourceText = new FText(Ar);
                TransformType = Ar.Read<ETransformType>();
            }
        }

        public class StringTableEntry : FTextHistory
        {
            public readonly FName TableId;
            public readonly string Key;
            public override string Text { get; }

            public StringTableEntry(FAssetArchive Ar)
            {
                TableId = Ar.ReadFName();
                Key = Ar.ReadFString();

                if (Ar.Owner.Provider!.TryLoadObject(TableId.Text, out UStringTable table) &&
                    table.StringTable.KeysToMetaData.TryGetValue(Key, out var t))
                {
                    Text = t;
                }
            }
        }

        public class TextGenerator : FTextHistory
        {
            public readonly FName GeneratorTypeID;
            public readonly byte[]? GeneratorContents;
            public override string Text => GeneratorTypeID.Text;

            public TextGenerator(FAssetArchive Ar)
            {
                GeneratorTypeID = Ar.ReadFName();
                if (!GeneratorTypeID.IsNone)
                {
                    // https://github.com/EpicGames/UnrealEngine/blob/4.26/Engine/Source/Runtime/Core/Private/Internationalization/TextHistory.cpp#L2916
                    // I don't understand what it does here
                }
            }
        }
    }

    public class FFormatArgumentValue : IUStruct
    {
        public EFormatArgumentType Type;
        public object Value;

        public FFormatArgumentValue(FAssetArchive Ar)
        {
            Type = Ar.Read<EFormatArgumentType>();
            Value = Type switch
            {
                EFormatArgumentType.Text => new FText(Ar),
                EFormatArgumentType.Int => Ar.Game == EGame.GAME_HogwartsLegacy ? Ar.Read<int>() : Ar.Read<long>(),
                EFormatArgumentType.UInt => Ar.Read<ulong>(),
                EFormatArgumentType.Double => Ar.Read<double>(),
                EFormatArgumentType.Float => Ar.Read<float>(),
                _ => throw new ParserException(Ar, $"{Type} argument not supported yet"),
            };
        }
    }

    public class FFormatArgumentData : IUStruct
    {
        public string ArgumentName;
        public FFormatArgumentValue ArgumentValue;

        public FFormatArgumentData(FAssetArchive Ar)
        {
            ArgumentName = Ar.ReadFString();
            ArgumentValue = new FFormatArgumentValue(Ar);
        }
    }

    public class FNumberFormattingOptions : IUStruct
    {
        private const int _DBL_DIG = 15;
        private const int _DBL_MAX_10_EXP = 308;

        public bool AlwaysSign;
        public bool UseGrouping;
        public ERoundingMode RoundingMode;
        public int MinimumIntegralDigits;
        public int MaximumIntegralDigits;
        public int MinimumFractionalDigits;
        public int MaximumFractionalDigits;

        public FNumberFormattingOptions()
        {
            AlwaysSign = false;
            UseGrouping = true;
            RoundingMode = ERoundingMode.HalfToEven;
            MinimumIntegralDigits = 1;
            MaximumIntegralDigits = _DBL_MAX_10_EXP + _DBL_DIG + 1;
            MinimumFractionalDigits = 0;
            MaximumFractionalDigits = 3;
        }

        public FNumberFormattingOptions(FAssetArchive Ar)
        {
            AlwaysSign = Ar.ReadBoolean();
            UseGrouping = Ar.ReadBoolean();
            RoundingMode = Ar.Read<ERoundingMode>();
            MinimumIntegralDigits = Ar.Read<int>();
            MaximumIntegralDigits = Ar.Read<int>();
            MinimumFractionalDigits = Ar.Read<int>();
            MaximumFractionalDigits = Ar.Read<int>();
        }
    }
}
