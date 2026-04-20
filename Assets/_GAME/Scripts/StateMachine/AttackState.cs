using UnityEngine;
using System.Collections.Generic;

public class AttackState : ICharacterState
{
    private CharacterBase target;
    private int pathIndex;
    private float repathTimer;
    private float chaseTimer;
    private Vector3 lastTargetPos;
    
    private readonly Dictionary<CharacterBase, float> targetBlacklist = new();
    private const float BlacklistDuration = 5f;

    private const float RepathInterval = 0.5f;
    private const float TargetMovedThresholdSq = 2f;
    private const float FleeChaseTimeout = 2f;

    public void SetTarget(CharacterBase t) => target = t;

    public void Enter(CharacterStateMachine sm)
    {
        repathTimer = 0f;
        chaseTimer = 0f;
        pathIndex = 0;
        lastTargetPos = Vector3.zero;
        
        CleanupBlacklist();
        
        if (target != null) BuildPathToTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        int mySwords = sm.MySwordCount;
        
        if (mySwords <= 0)
        {
            if (target != null) sm.Flee.SetThreat(target);
            sm.ChangeState(sm.Flee);
            return;
        }

        if (target == null || target.CurrentHp <= 0f || !target.gameObject.activeInHierarchy)
        {
            sm.ChangeState(sm.Wander);
            return;
        }

        if (mySwords <= 3 && target.SwordCount >= mySwords)
        {
            sm.Flee.SetThreat(target);
            sm.ChangeState(sm.Flee);
            return;
        }

        if (target.GetStateMachine()?.CurrentState is FleeState)
        {
            chaseTimer += deltaTime;
            if (chaseTimer >= FleeChaseTimeout)
            {
                AddToBlacklist(target);
                sm.ChangeState(sm.Wander);
                return;
            }
        }
        else
        {
            chaseTimer = 0f;
        }

        Vector3 targetPos = target.Position;
        float dx = targetPos.x - sm.CachedPosition.x;
        float dy = targetPos.y - sm.CachedPosition.y;
        float distSq = dx * dx + dy * dy;

        if (distSq > sm.VisionRadiusSq * 1.2f)
        {
            AddToBlacklist(target);
            sm.ChangeState(sm.Wander);
            return;
        }

        repathTimer -= deltaTime;
        dx = targetPos.x - lastTargetPos.x;
        dy = targetPos.y - lastTargetPos.y;
        if (repathTimer <= 0f || dx * dx + dy * dy > TargetMovedThresholdSq)
        {
            repathTimer = RepathInterval;
            BuildPathToTarget(sm);
        }

        sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime);
    }

    public void Exit(CharacterStateMachine sm) => target = null;

    public bool IsBlacklisted(CharacterBase character)
    {
        if (character == null) return false;
        if (!targetBlacklist.TryGetValue(character, out float expireTime)) return false;
        return Time.time < expireTime;
    }

    public void RemoveFromBlacklist(CharacterBase character)
    {
        if (character != null)
            targetBlacklist.Remove(character);
    }

    private void AddToBlacklist(CharacterBase character)
    {
        if (character != null)
            targetBlacklist[character] = Time.time + BlacklistDuration;
    }

    private void CleanupBlacklist()
    {
        float currentTime = Time.time;
        var toRemove = new List<CharacterBase>();
        
        foreach (var kvp in targetBlacklist)
        {
            if (currentTime >= kvp.Value)
                toRemove.Add(kvp.Key);
        }
        
        foreach (var key in toRemove)
            targetBlacklist.Remove(key);
    }

    private void BuildPathToTarget(CharacterStateMachine sm)
    {
        if (target == null) return;
        lastTargetPos = target.Position;
        pathIndex = 0;
        if (sm.Pathfinder != null)
            sm.Pathfinder.FindPath(sm.CachedPosition, target.Position, sm.PathBuffer);
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(target.Position);
        }
    }
}
