using System;

public class ConditionNode : BTNode
{
    private Func<bool> condition;

    public ConditionNode(Func<bool> condition)
    {
        this.condition = condition;
    }

    public override string DebugName => "Condition: " + condition.Method.Name + "?";

    public override NodeState Tick()
    {
        return condition()
            ? NodeState.Success
            : NodeState.Failure;
    }
}