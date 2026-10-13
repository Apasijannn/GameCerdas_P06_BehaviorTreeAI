using System.Collections.Generic;

public class SelectorNode : BTNode
{
    private List<BTNode> children;

    public SelectorNode(List<BTNode> children)
    {
        this.children = children;
    }

    public override IReadOnlyList<BTNode> DebugChildren => children;

    public override NodeState Tick()
    {
        foreach (BTNode child in children)
        {
            NodeState state = child.Evaluate();

            if (state == NodeState.Success)
                return NodeState.Success;

            if (state == NodeState.Running)
                return NodeState.Running;
        }

        return NodeState.Failure;
    }
}