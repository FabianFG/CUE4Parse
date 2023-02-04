using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

[StructFallback]
public class FAnimNotifyEvent : FAnimLinkableElement
{
	public float TriggerTimeOffset;
	public float EndTriggerTimeOffset;
	public float TriggerWeightThreshold;
	public FName NotifyName;
	public FPackageIndex Notify;
	public FPackageIndex NotifyStateClass;
	public float Duration;
	public FAnimLinkableElement EndLink;
	public bool bConvertedFromBranchingPoint;
	public EMontageNotifyTickType MontageTickType;
	public float NotifyTriggerChance;
	public EMontageNotifyTickType NotifyFilterType;
	public int NotifyFilterLOD;
	public bool bTriggerOnDedicatedServer;
	public bool bTriggerOnFollower;
	public int TrackIndex;

	public FAnimNotifyEvent(FStructFallback fallback) : base(fallback)
	{
		TriggerTimeOffset = fallback.GetOrDefault<float>(nameof(TriggerTimeOffset));
		EndTriggerTimeOffset = fallback.GetOrDefault<float>(nameof(EndTriggerTimeOffset));
		TriggerWeightThreshold = fallback.GetOrDefault<float>(nameof(TriggerWeightThreshold));
		NotifyName = fallback.GetOrDefault<FName>(nameof(NotifyName));
		Notify = fallback.GetOrDefault<FPackageIndex>(nameof(Notify));
		NotifyStateClass = fallback.GetOrDefault<FPackageIndex>(nameof(NotifyStateClass));
		Duration = fallback.GetOrDefault<float>(nameof(Duration));
		EndLink = fallback.GetOrDefault<FAnimLinkableElement>(nameof(EndLink));
		bConvertedFromBranchingPoint = fallback.GetOrDefault<bool>(nameof(bConvertedFromBranchingPoint));
		MontageTickType = fallback.GetOrDefault<EMontageNotifyTickType>(nameof(MontageTickType));
		NotifyTriggerChance = fallback.GetOrDefault<float>(nameof(NotifyTriggerChance));
		NotifyFilterType = fallback.GetOrDefault<EMontageNotifyTickType>(nameof(NotifyFilterType));
		NotifyFilterLOD = fallback.GetOrDefault<int>(nameof(NotifyFilterLOD));
		bTriggerOnDedicatedServer = fallback.GetOrDefault<bool>(nameof(bTriggerOnDedicatedServer));
		bTriggerOnFollower = fallback.GetOrDefault<bool>(nameof(bTriggerOnFollower));
		TrackIndex = fallback.GetOrDefault<int>(nameof(TrackIndex));
	}
}

public enum EMontageNotifyTickType
{
	Queued,
	BranchingPoint,
}