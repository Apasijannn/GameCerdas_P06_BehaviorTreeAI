using System.Collections.Generic;
using UnityEngine;

public class CooldownDecorator : BTNode
{
    private BTNode child;
    private float cooldown;
    private float nextAllowedTime;

    public CooldownDecorator(
        BTNode child,
        float cooldown)
    {
        this.child = child;
        this.cooldown = cooldown;
        nextAllowedTime = 0f;
    }

    public override string DebugName =>
        Time.time < nextAllowedTime
            ? $"Cooldown ({nextAllowedTime - Time.time:0.0}s)"
            : $"Cooldown ({cooldown:0.0}s)";

    public override IReadOnlyList<BTNode> DebugChildren => new[] { child };

    public override NodeState Tick()
    {
        if (Time.time < nextAllowedTime)
        {
            return NodeState.Running;
        }

        NodeState state = child.Evaluate();

        if (state == NodeState.Success)
        {
            nextAllowedTime =
                Time.time + cooldown;
        }

        return state;
    }
}