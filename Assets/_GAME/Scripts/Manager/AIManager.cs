using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized manager that schedules staggered AI evaluations (round-robin, max N per frame)
/// and batch-updates movement for every registered AIBrain each frame.
/// </summary>
public class AIManager : Singleton<AIManager>
{
    // ── Serialized tuning fields ──
    [SerializeField] private int maxEvaluationsPerFrame = 20;
    [SerializeField] private float evaluationInterval = 0.3f;

    // ── Internal state ──
    private readonly List<AIBrain> brains = new();
    private readonly List<AIBrain> pendingAdd = new();
    private readonly List<AIBrain> pendingRemove = new();
    private readonly HashSet<AIBrain> brainSet = new();
    private bool isUpdating;
    private int roundRobinIndex;

    // ── Properties ──
    public int BrainCount => brains.Count;

    // ── Lifecycle ──

    protected override void Awake()
    {
        base.Awake();
    }

    // ── Registration ──

    public void Register(AIBrain brain)
    {
        if (brain == null) return;

        // Idempotent: skip if already registered or already pending add
        if (brainSet.Contains(brain)) return;

        if (isUpdating)
        {
            // Defer until next frame flush
            if (!pendingAdd.Contains(brain))
                pendingAdd.Add(brain);
            return;
        }

        brains.Add(brain);
        brainSet.Add(brain);
    }

    public void Deregister(AIBrain brain)
    {
        if (brain == null) return;

        // Silently ignore if not registered
        if (!brainSet.Contains(brain)) return;

        if (isUpdating)
        {
            // Defer until next frame flush
            if (!pendingRemove.Contains(brain))
                pendingRemove.Add(brain);
            return;
        }

        brains.Remove(brain);
        brainSet.Remove(brain);

        // Clamp round-robin index after removal
        if (brains.Count > 0 && roundRobinIndex >= brains.Count)
            roundRobinIndex = roundRobinIndex % brains.Count;
        else if (brains.Count == 0)
            roundRobinIndex = 0;
    }

    // ── Update loop ──

    private void Update()
    {
        // Flush pending additions
        for (int i = 0; i < pendingAdd.Count; i++)
        {
            AIBrain brain = pendingAdd[i];
            if (!brainSet.Contains(brain))
            {
                brains.Add(brain);
                brainSet.Add(brain);
            }
        }
        pendingAdd.Clear();

        // Flush pending removals
        for (int i = 0; i < pendingRemove.Count; i++)
        {
            AIBrain brain = pendingRemove[i];
            if (brainSet.Contains(brain))
            {
                brains.Remove(brain);
                brainSet.Remove(brain);
            }
        }
        pendingRemove.Clear();

        // Clamp round-robin index after removals
        if (brains.Count > 0 && roundRobinIndex >= brains.Count)
            roundRobinIndex = roundRobinIndex % brains.Count;
        else if (brains.Count == 0)
            roundRobinIndex = 0;

        int count = brains.Count;
        if (count == 0) return;

        // ── Staggered evaluation: round-robin up to maxEvaluationsPerFrame ──
        isUpdating = true;

        int toEval = Mathf.Min(maxEvaluationsPerFrame, count);
        for (int i = 0; i < toEval; i++)
        {
            brains[roundRobinIndex].Evaluate();
            roundRobinIndex = (roundRobinIndex + 1) % count;
        }

        // ── Batch movement: every registered brain, every frame ──
        float dt = Time.deltaTime;
        for (int i = 0; i < count; i++)
        {
            brains[i].ExecuteMovement(dt);
        }

        isUpdating = false;
    }
}
