using System;
using System.Collections.Generic;
using UnityEngine;

// Composite node hybrid: memilih SATU child berdasarkan skor utility tertinggi.
// Dilengkapi Action Commitment & Hysteresis agar tidak gonta-ganti aksi tiap frame.
public class UtilitySelectorNode : BTNode
{
    public class Option
    {
        public string name;
        public BTNode node;
        public Func<float> score;
        public float lastScore;

        public Option(string name, BTNode node, Func<float> score)
        {
            this.name = name;
            this.node = node;
            this.score = score;
        }
    }

    private readonly List<Option> options;
    private readonly float minActionDuration;
    private readonly float hysteresis;

    private Option current;
    private float currentStartTime;

    public IReadOnlyList<Option> Options => options;
    public string CurrentOptionName => current != null ? current.name : "-";

    public override string DebugName => "Utility Selector";
    public override IReadOnlyList<BTNode> DebugChildren
    {
        get
        {
            var list = new List<BTNode>();
            foreach (Option o in options) list.Add(o.node);
            return list;
        }
    }

    // Skor terakhir untuk child tertentu (untuk label debugger)
    public bool TryGetScore(BTNode child, out float score, out string optionName)
    {
        foreach (Option o in options)
            if (o.node == child) { score = o.lastScore; optionName = o.name; return true; }
        score = 0f; optionName = null;
        return false;
    }

    public UtilitySelectorNode(List<Option> options, float minActionDuration = 0.5f, float hysteresis = 0.1f)
    {
        this.options = options;
        this.minActionDuration = minActionDuration;
        this.hysteresis = hysteresis;
    }

    public override NodeState Tick()
    {
        // 1. Hitung semua skor
        Option best = null;
        foreach (Option o in options)
        {
            o.lastScore = Mathf.Clamp01(o.score());
            if (best == null || o.lastScore > best.lastScore)
                best = o;
        }

        if (best == null || best.lastScore <= 0f)
        {
            current = null;
            return NodeState.Failure;
        }

        // 2. Commitment + Hysteresis
        if (current == null)
        {
            Switch(best);
        }
        else if (best != current)
        {
            bool committed = Time.time - currentStartTime < minActionDuration;
            bool clearlyBetter = best.lastScore >= current.lastScore + hysteresis;

            if (current.lastScore <= 0f || (!committed && clearlyBetter))
                Switch(best);
        }

        // 3. Jalankan aksi terpilih
        return current.node.Evaluate();
    }

    private void Switch(Option o)
    {
        current = o;
        currentStartTime = Time.time;
    }
}
