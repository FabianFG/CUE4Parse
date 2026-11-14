using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.UE4.Wwise.Objects.Actions;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

namespace CUE4Parse.UE4.Wwise;

public partial class WwiseProvider
{
    private sealed class EventSoundResolver(WwiseProvider provider, List<WwiseExtractedSound> results, string ownerDirectory, string? debugName, IEnumerable<uint> visitedMedia)
    {
        private readonly record struct GameSyncAssignment(EAkGroupType Type, uint GroupId, uint Value);

        private readonly WwiseProvider _provider = provider;
        private readonly List<WwiseExtractedSound> _results = results;
        private readonly string _ownerDirectory = ownerDirectory;
        private readonly string? _debugName = debugName;

        private readonly HashSet<(uint Id, Hierarchy Hierarchy)> _visitedHierarchies = [];
        private readonly HashSet<uint> _visitedWemIds = [.. visitedMedia];
        private readonly List<GameSyncAssignment> _gameSyncAssignments = [];

        public void Resolve(uint eventId) => Traverse(eventId, eventId);

        private void Traverse(uint id, uint eventId)
        {
            foreach (var hierarchy in _provider.GetHierarchiesById(id))
            {
                if (!_visitedHierarchies.Add((id, hierarchy)))
                    continue;

                switch (hierarchy.Data)
                {
                    case HierarchySoundSfxVoice sound:
                        if (sound.Source is { Plugin.Type: EAkPluginType.Codec })
                            SaveWem(sound.Source.SourceId);
                        else
                            Traverse(sound.Source.SourceId, eventId);
                        break;
                    case HierarchyMusicRandomSequenceContainer container:
                        TraverseChildren(container.ChildIds, eventId);
                        break;
                    case HierarchyMusicSwitchContainer container:
                        TraverseMusicSwitch(container, eventId);
                        break;
                    case HierarchyMusicTrack track:
                        foreach (var item in track.Playlist)
                            SaveWem(item.SourceId);
                        break;
                    case HierarchyMusicSegment segment:
                        TraverseChildren(segment.ChildIds, eventId);
                        break;
                    case HierarchyRandomSequenceContainer container:
                        TraverseChildren(container.ChildIds, eventId);
                        break;
                    case HierarchySwitchContainer container:
                        TraverseSwitch(container, eventId);
                        break;
                    case HierarchyLayerContainer container:
                        TraverseChildren(container.ChildIds, eventId);
                        break;
                    case HierarchyFxCustom effect:
                        foreach (var media in effect.MediaList)
                            SaveWem(media.SourceId);
                        break;
                    case HierarchyEvent eventContainer:
                        TraverseEvent(eventContainer, eventId);
                        break;
                    default:
                        Log.Warning("Unhandled hierarchy type {0}, while traversing through Event {1}", hierarchy.Type, eventId);
                        break;
                }
            }
        }

        private void TraverseChildren(IEnumerable<uint> childIds, uint eventId)
        {
            foreach (var childId in childIds)
            {
                Traverse(childId, eventId);
            }
        }

        private void TraverseEvent(HierarchyEvent eventContainer, uint eventId)
        {
            var contextStart = _gameSyncAssignments.Count;
            try
            {
                var actions = eventContainer.EventActionIds
                    .Select(TryGetEventAction)
                    .OfType<HierarchyEventAction>()
                    .ToArray();

                // Wwise states are global, capture the complete state context before resolving play actions
                // then resolve any music containers affected by those states
                foreach (var action in actions)
                {
                    if (action.EventActionType is EAkActionType.SetState && action.ActionData is CAkActionSetState state)
                    {
                        RecordGameSyncAssignment(EAkGroupType.State, state.StateGroupId, state.TargetStateId);
                    }
                }

                foreach (var action in actions.Where(x => x.EventActionType is not EAkActionType.SetState)) // SetState wouldn't resolve audio and is handled above
                {
                    switch (action.EventActionType, action.ActionData)
                    {
                        case (EAkActionType.SetSwitch, CAkActionSetSwitch value):
                            RecordGameSyncAssignment(EAkGroupType.Switch, value.SwitchGroupId, value.SwitchStateId);
                            break;
                        default:
                            Traverse(action.ReferencedId, eventId);
                            break;
                    }
                }

                TraverseStateConsumers(contextStart, eventId);
            }
            finally
            {
                _gameSyncAssignments.RemoveRange(contextStart, _gameSyncAssignments.Count - contextStart);
            }
        }

        private HierarchyEventAction? TryGetEventAction(uint actionId) =>
            _provider._wwiseHierarchyTables.TryGetValue(actionId, out var hierarchy) ? hierarchy.Data as HierarchyEventAction : null;

        private void TraverseStateConsumers(int contextStart, uint eventId)
        {
            HashSet<uint> visitedConsumers = [];
            foreach (var state in _gameSyncAssignments.Skip(contextStart).Where(x => x.Type is EAkGroupType.State))
            {
                if (!_provider._musicSwitchContainersByGameSync.TryGetValue((state.Type, state.GroupId), out var consumerIds))
                    continue;

                foreach (var consumerId in consumerIds)
                {
                    if (visitedConsumers.Add(consumerId))
                    {
                        Traverse(consumerId, eventId);
                    }
                }
            }
        }

        private void TraverseMusicSwitch(HierarchyMusicSwitchContainer container, uint eventId)
        {
            if (container.Arguments.Any(x => x.GroupType is EAkGroupType.State && GetGameSyncValue(x).HasValue))
            {
                foreach (var root in container.DecisionTree.Nodes)
                {
                    TraverseDecisionTree(root, 0, container.Arguments, eventId);
                }
                return;
            }

            TraverseChildren(container.ChildIds, eventId);
            foreach (var root in container.DecisionTree.Nodes)
            {
                foreach (var child in root.Children)
                {
                    TraverseDecisionTreeUnfiltered(child, eventId);
                }
            }
        }

        private void TraverseDecisionTree(AkDecisionTreeNode node, int argumentIndex, AkGameSync[] arguments, uint eventId)
        {
            if (node.Children.Length == 0)
            {
                if (node.AudioNodeId != 0)
                {
                    Traverse(node.AudioNodeId, eventId);
                }
                return;
            }

            var children = SelectDecisionTreeChildren(node, argumentIndex, arguments);
            foreach (var child in children)
            {
                TraverseDecisionTree(child, argumentIndex + 1, arguments, eventId);
            }
        }

        private IEnumerable<AkDecisionTreeNode> SelectDecisionTreeChildren(AkDecisionTreeNode node, int argumentIndex, AkGameSync[] arguments)
        {
            var value = argumentIndex < arguments.Length ? GetGameSyncValue(arguments[argumentIndex]) : null;
            if (!value.HasValue)
                return node.Children;

            var exactMatches = node.Children.Where(x => x.Key == value.Value).ToArray();
            return exactMatches.Length > 0 ? exactMatches : node.Children.Where(x => x.Key == 0);
        }

        private void TraverseDecisionTreeUnfiltered(AkDecisionTreeNode node, uint eventId)
        {
            if (node.AudioNodeId != 0)
            {
                Traverse(node.AudioNodeId, eventId);
            }

            foreach (var child in node.Children)
            {
                TraverseDecisionTreeUnfiltered(child, eventId);
            }
        }

        private void TraverseSwitch(HierarchySwitchContainer container, uint eventId)
        {
            if (GetGameSyncValue(EAkGroupType.Switch, container.GroupId) is { } value)
            {
                TraverseSwitchValue(container, value, eventId);
            }
            else if (container.DefaultSwitch == 0 || !_gameSyncAssignments.Any(x => x.Type is EAkGroupType.Switch))
            {
                TraverseChildren(container.ChildIds, eventId);
            }
            else
            {
                TraverseSwitchValue(container, container.DefaultSwitch, eventId);
            }
        }

        private void TraverseSwitchValue(HierarchySwitchContainer container, uint value, uint eventId)
        {
            foreach (var package in container.SwitchPackages.Where(x => x.SwitchId == value && x.NodeIds is not null))
            {
                TraverseChildren(package.NodeIds, eventId);
            }
        }

        private void RecordGameSyncAssignment(EAkGroupType type, uint groupId, uint value) =>
            _gameSyncAssignments.Add(new GameSyncAssignment(type, groupId, value));

        private uint? GetGameSyncValue(AkGameSync gameSync) => GetGameSyncValue(gameSync.GroupType, gameSync.GroupId);
        private uint? GetGameSyncValue(EAkGroupType type, uint groupId)
        {
            for (var i = _gameSyncAssignments.Count - 1; i >= 0; i--)
            {
                var candidate = _gameSyncAssignments[i];
                if (candidate.Type != type || candidate.GroupId != groupId)
                    continue;

                return candidate.Value;
            }

            return null;
        }

        private void SaveWem(uint wemId)
        {
            if (!_visitedWemIds.Add(wemId))
                return;

            var fileName = wemId.ToString();
            var hasLooseFile = _provider._looseWemFilesLookup.TryGetValue(wemId, out var wemFile);
            var hasEncodedMedia = _provider._wwiseEncodedMedia.TryGetValue(fileName, out var wemData);
            if (!hasLooseFile && !hasEncodedMedia)
            {
                Log.Error("Failed to load data for '{WemId}' wem file during event resolution", wemId);
                return;
            }

            if (!string.IsNullOrEmpty(_debugName) && !_debugName.Equals("None"))
                fileName = $"{_debugName} ({fileName})";

            var outputPath = Path.Combine(_ownerDirectory, fileName).TrimStart('/');
            _results.Add(new WwiseExtractedSound
            {
                OutputPath = outputPath.Replace('\\', '/'),
                Extension = "wem",
                Data = wemFile is { IsValid: true } ? wemFile : wemData,
            });
        }
    }
}
