using System;

public class ActionNode : BTNode
{
    private Func<NodeState> action;

    public ActionNode(Func<NodeState> action)
    {
        this.action = action;
    }

    public override string DebugName => "Action: " + action.Method.Name;

    public override NodeState Tick()
    {
        return action();
    }
}