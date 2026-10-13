using System.Collections.Generic;
using UnityEngine;

public enum NodeState
{
    Success,
    Failure,
    Running
}

public abstract class BTNode
{
    public abstract NodeState Tick();

    // ==================================================
    // DEBUG (dipakai On-screen BT Debugger)
    // ==================================================

    // Status terakhir & frame terakhir node ini dievaluasi
    public NodeState LastState { get; private set; }
    public int LastTickFrame { get; private set; } = -1;

    // Nama tampilan node di debugger
    public virtual string DebugName => GetType().Name.Replace("Node", "");

    // Child node (untuk menggambar tree)
    public virtual IReadOnlyList<BTNode> DebugChildren => System.Array.Empty<BTNode>();

    public bool WasTickedThisFrame => LastTickFrame == Time.frameCount;

    // Panggil Evaluate() (bukan Tick()) dari parent agar status tercatat.
    public NodeState Evaluate()
    {
        LastState = Tick();
        LastTickFrame = Time.frameCount;
        return LastState;
    }
}
