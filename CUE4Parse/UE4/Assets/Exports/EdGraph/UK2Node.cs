using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine.EdGraph;

namespace CUE4Parse.UE4.Assets.Exports.EdGraph;

public class UK2Node : UEdGraphNode;

public class UK2Node_EditablePinBase : UK2Node
{
    public FUserPinInfo[]? SerializedItems;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SerializedItems = Ar.ReadArray(() => new FUserPinInfo(Ar));
    }
}

public class UK2Node_IfThenElse : UK2Node;
public class UK2Node_Tunnel : UK2Node;
public class UK2Node_CallFunction : UK2Node;
public class UEdGraphNode_Comment : UK2Node;
public class UK2Node_DynamicCast : UK2Node;
public class UK2Node_FunctionEntry : UK2Node;
public class UK2Node_FunctionResult : UK2Node;

public class UAnimGraphNode_Base : UK2Node;
public class UAnimGraphNode_BlendListBase : UK2Node;

public class UAnimGraphNode_BlendListByBool : UAnimGraphNode_BlendListBase;
public class UAnimGraphNode_BlendListByInt : UAnimGraphNode_BlendListBase;

public class UAnimGraphNode_ComponentToLocalSpace : UAnimGraphNode_Base;
public class UAnimGraphNode_LayeredBoneBlend : UAnimGraphNode_Base;
public class UAnimGraphNode_LegIK : UAnimGraphNode_Base;
public class UAnimGraphNode_LocalToComponentSpace : UAnimGraphNode_Base;
public class UAnimGraphNode_LookAt : UAnimGraphNode_Base;
public class UAnimGraphNode_ModifyBone : UAnimGraphNode_Base;
public class UAnimGraphNode_Root : UAnimGraphNode_Base;
public class UAnimGraphNode_RotationMultiplier : UAnimGraphNode_Base;
public class UAnimGraphNode_RotationOffsetBlendSpace : UAnimGraphNode_Base;
public class UAnimGraphNode_SaveCachedPose : UAnimGraphNode_Base;
public class UAnimGraphNode_SequencePlayer : UAnimGraphNode_Base;
public class UAnimGraphNode_Slot : UAnimGraphNode_Base;
public class UAnimGraphNode_StateMachine : UAnimGraphNode_Base;
public class UAnimGraphNode_StateResult : UAnimGraphNode_Base;
public class UAnimGraphNode_TransitionResult : UAnimGraphNode_Base;
public class UAnimGraphNode_TwoBoneIK : UAnimGraphNode_Base;
public class UAnimGraphNode_UseCachedPose : UAnimGraphNode_Base;

public class UAnimStateEntryNode : UK2Node;
public class UAnimStateNode : UK2Node;
public class UAnimStateTransitionNode : UK2Node;

public class UK2Node_AddDelegate : UK2Node;
public class UK2Node_AnimGetter : UK2Node;
public class UK2Node_CallDelegate : UK2Node;
public class UK2Node_CommutativeAssociativeBinaryOperator : UK2Node;
public class UK2Node_Composite : UK2Node;
public class UK2Node_CreateDelegate : UK2Node;
public class UK2Node_CustomEvent : UK2Node;
public class UK2Node_Event : UK2Node;
public class UK2Node_ExecutionSequence : UK2Node;
public class UK2Node_Knot : UK2Node;
public class UK2Node_MacroInstance : UK2Node;
public class UK2Node_Select : UK2Node;
public class UK2Node_SpawnActorFromClass : UK2Node;
public class UK2Node_SwitchEnum : UK2Node;
public class UK2Node_VariableGet : UK2Node;