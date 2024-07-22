using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Objects.GameplayTags;

public readonly struct FGameplayTagContainer : IUStruct, IEnumerable<FGameplayTag>
{
    public readonly FGameplayTag[] GameplayTags;

    public FGameplayTagContainer(FAssetArchive Ar)
    {
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.GAMEPLAY_TAG_CONTAINER_TAG_TYPE_CHANGE)
        {
            GameplayTags = Ar.ReadArray(() => new FGameplayTag(Ar));
        }
        else
        {
            GameplayTags = Ar.ReadArray(() => new FGameplayTag(Ar.ReadFName()));
        }
    }

    public FGameplayTagContainer(params FGameplayTag[] gameplayTags)
    {
        GameplayTags = gameplayTags;
    }

    public bool HasTag(FGameplayTag tagToCheck)
    {
        return tagToCheck.IsValid() && GameplayTags.Contains(tagToCheck);
    }

    public bool HasAny(FGameplayTagContainer containerToCheck)
    {
        if (containerToCheck.IsEmpty()) return false;

        foreach (var otherTag in containerToCheck.GameplayTags)
        {
            if (HasTag(otherTag)) return true;
        }

        return false;
    }

    public bool HasAll(FGameplayTagContainer containerToCheck)
    {
        if (containerToCheck.IsEmpty()) return true;

        foreach (var otherTag in containerToCheck.GameplayTags)
        {
            if (!HasTag(otherTag)) return false;
        }

        return true;
    }

    public bool MatchesQuery(FGameplayTagQuery query)
    {
        return query.Matches(this);
    }

    public bool IsValid()
    {
        return GameplayTags.Length > 0;
    }

    public bool IsEmpty()
    {
        return GameplayTags.Length == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FName? GetValue(string category) => GameplayTags.FirstOrDefault(it => it.TagName.Text.StartsWith(category)).TagName;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<FGameplayTag> GetEnumerator() => ((IEnumerable<FGameplayTag>) GameplayTags).GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GameplayTags.GetEnumerator();

    public override string ToString() => string.Join(", ", GameplayTags);
}

[StructFallback]
[JsonConverter(typeof(FGameplayTagConverter))]
public struct FGameplayTag
{
    public FName TagName;

    public FGameplayTag(FAssetArchive Ar)
    {
        TagName = Ar.ReadFName();
    }

    public FGameplayTag(FStructFallback fallback)
    {
        TagName = fallback.Get<FName>(nameof(TagName));
    }

    public FGameplayTag(FName tagName)
    {
        TagName = tagName;
    }

    public bool IsValid()
    {
        return !TagName.IsNone;
    }

    public static bool operator ==(FGameplayTag a, FGameplayTag b) 
    {
        return a.TagName.CompareTo(b.TagName) == 0;
    }

    public static bool operator !=(FGameplayTag a, FGameplayTag b)
    {
        return a.TagName.CompareTo(b.TagName) != 0;
    }

    public bool Equals(FGameplayTag other)
    {
        return TagName.CompareTo(other.TagName) == 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is FGameplayTag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return TagName.GetHashCode();
    }

    public override string ToString()
    {
        return TagName.Text;
    }
}

public enum EGameplayTagQueryStreamVersion : byte
{
    InitialVersion = 0,

    VersionPlusOne,
    LatestVersion = VersionPlusOne - 1
}

[StructFallback]
public class FGameplayTagQuery
{
    public EGameplayTagQueryStreamVersion TokenStreamVersion;
    public FGameplayTag[] TagDictionary;
    public byte[] QueryTokenStream;
    public string UserDescription;
    public string AutoDescription;

    public FGameplayTagQuery(FStructFallback fallback)
    {
        TokenStreamVersion = fallback.GetOrDefault<EGameplayTagQueryStreamVersion>(nameof(TokenStreamVersion));
        TagDictionary = fallback.GetOrDefault<FGameplayTag[]>(nameof(TagDictionary));
        QueryTokenStream = fallback.GetOrDefault<byte[]>(nameof(QueryTokenStream));
        UserDescription = fallback.GetOrDefault<string>(nameof(UserDescription));
        AutoDescription = fallback.GetOrDefault<string>(nameof(AutoDescription));
    }

    public bool Matches(FGameplayTagContainer tags)
    {
        var evaluator = new FQueryEvaluator(this);
        return evaluator.Eval(tags);
    }

    public FGameplayTag GetTagFromIndex(int idx)
    {
        return TagDictionary[idx];
    }
}

public enum EGameplayTagQueryExprType
{
    Undefined = 0,
    AnyTagsMatch,
    AllTagsMatch,
    NoTagsMatch,
    AnyExprMatch,
    AllExprMatch,
    NoExprMatch,
}

public class FQueryEvaluator
{
    private readonly FGameplayTagQuery Query;
    private int CurStreamIdx;
    private EGameplayTagQueryStreamVersion Version;
    private bool bReadError;

    public FQueryEvaluator(FGameplayTagQuery query)
    {
        Query = query;
    }

    public bool Eval(FGameplayTagContainer tags)
    {
        CurStreamIdx = 0;

        Version = (EGameplayTagQueryStreamVersion) GetToken();
        if (bReadError) return false;

        var returnValue = false;
        var hasRootExpression = GetToken() == 1;
        if (!bReadError && hasRootExpression)
        {
            returnValue = EvalExpr(tags);
        }

        return returnValue && CurStreamIdx == Query.QueryTokenStream.Length;
    }

    private bool EvalExpr(FGameplayTagContainer tags, bool skip = false)
    {
        var exprType = (EGameplayTagQueryExprType) GetToken();

        return exprType switch
        {
            EGameplayTagQueryExprType.AnyTagsMatch => EvalAnyTagsMatch(tags, skip),
            EGameplayTagQueryExprType.AllTagsMatch => EvalAllTagsMatch(tags, skip),
            EGameplayTagQueryExprType.NoTagsMatch => EvalNoTagsMatch(tags, skip),
            EGameplayTagQueryExprType.AnyExprMatch => EvalAnyExprMatch(tags, skip),
            EGameplayTagQueryExprType.AllExprMatch => EvalAllExprMatch(tags, skip),
            EGameplayTagQueryExprType.NoExprMatch => EvalNoExprMatch(tags, skip),
            _ => false
        };
    }

    private bool EvalAnyTagsMatch(FGameplayTagContainer tags, bool skip)
    {
        var shortCircuit = skip;
        var result = false;

        var numTags = GetToken();
        if (bReadError) return false;

        for (var idx = 0; idx < numTags; idx++)
        {
            var tagIdx = GetToken();
            if (bReadError) return false;

            if (shortCircuit) continue;

            var tag = Query.GetTagFromIndex(tagIdx);

            var bHasTag = tags.HasTag(tag);
            if (bHasTag)
            {
                shortCircuit = true;
                result = true;
            }
        }

        return result;
    }

    private bool EvalAllTagsMatch(FGameplayTagContainer tags, bool skip)
    {
        var shortCircuit = skip;
        var result = true;

        var numTags = GetToken();
        if (bReadError) return false;

        for (var idx = 0; idx < numTags; idx++)
        {
            var tagIdx = GetToken();
            if (bReadError) return false;

            if (shortCircuit) continue;

            var tag = Query.GetTagFromIndex(tagIdx);

            var bHasTag = tags.HasTag(tag);
            if (!bHasTag)
            {
                shortCircuit = true;
                result = false;
            }
        }

        return result;
    }

    private bool EvalNoTagsMatch(FGameplayTagContainer tags, bool skip)
    {
        var shortCircuit = skip;
        var result = true;

        var numTags = GetToken();
        if (bReadError) return false;

        for (var idx = 0; idx < numTags; idx++)
        {
            var tagIdx = GetToken();
            if (bReadError) return false;

            if (shortCircuit) continue;

            var tag = Query.GetTagFromIndex(tagIdx);

            var bHasTag = tags.HasTag(tag);
            if (bHasTag)
            {
                shortCircuit = true;
                result = false;
            }
        }

        return result;
    }

    private bool EvalAnyExprMatch(FGameplayTagContainer tags, bool skip)
    {
        var shortCircuit = skip;
        var result = false;

        var numExprs = GetToken();
        if (bReadError) return false;

        for (var idx = 0; idx < numExprs; idx++)
        {
            var exprResult = EvalExpr(tags, shortCircuit);
            if (shortCircuit) continue;
            if (!exprResult) continue;

            shortCircuit = true;
            result = true;
        }

        return result;
    }

    private bool EvalAllExprMatch(FGameplayTagContainer tags, bool skip)
    {
        var shortCircuit = skip;
        var result = true;

        var numExprs = GetToken();
        if (bReadError) return false;

        for (var idx = 0; idx < numExprs; idx++)
        {
            var exprResult = EvalExpr(tags, shortCircuit);
            if (shortCircuit) continue;
            if (exprResult) continue;

            shortCircuit = true;
            result = false;
        }

        return result;
    }

    private bool EvalNoExprMatch(FGameplayTagContainer tags, bool skip)
    {
        var shortCircuit = skip;
        var result = true;

        var numExprs = GetToken();
        if (bReadError) return false;

        for (var idx = 0; idx < numExprs; idx++)
        {
            var exprResult = EvalExpr(tags, shortCircuit);
            if (shortCircuit) continue;
            if (!exprResult) continue;

            shortCircuit = true;
            result = false;
        }

        return result;
    }

    private byte GetToken()
    {
        if (CurStreamIdx >= 0 && CurStreamIdx < Query.QueryTokenStream.Length)
        {
            return Query.QueryTokenStream[CurStreamIdx++];
        }

        Log.Error("Failed to parse FGameplayQuery!");
        bReadError = true;
        return 0;
    }

}

public static class FGameplayTagContainerUtility
{
    public static bool TryGetGameplayTag(this IEnumerable<FGameplayTag> gameplayTags, string startWith, out FName gameplayTag)
    {
        foreach (var tag in gameplayTags)
        {
            if (!tag.IsValid() || !tag.TagName.Text.StartsWith(startWith)) continue;

            gameplayTag = tag.TagName;
            return true;
        }

        gameplayTag = default;
        return false;
    }

    public static IList<string> GetAllGameplayTags(this IEnumerable<FGameplayTag> gameplayTags, params string[] startWith)
    {
        return (from tag in gameplayTags where tag.IsValid() from s in startWith where tag.TagName.Text.StartsWith(s) select tag.TagName.Text).ToList();
    }
}
