using UnityEngine;

public class AttackState : ICharacterState
{
    private CharacterBase target;
    private int pathIndex;
    private float repathTimer;
    private float chaseTimer;

    private const float RepathInterval = 0.5f;
    private const float FleeChaseTimeout = 2f;
    private const float MinAttackDistance = 1.5f;
    private const float MinAttackDistanceSq = MinAttackDistance * MinAttackDistance;
    private const float ChaseSpeedBonus = 0.5f;

    public void SetTarget(CharacterBase t) => target = t;
    public CharacterBase GetTarget() => target;

    public void Enter(CharacterStateMachine sm)
    {
        repathTimer = 0f;
        chaseTimer = 0f;
        pathIndex = 0;
        if (target != null) BuildPathToTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if (sm.Owner.IsKnockedBack) return;

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
            if ((chaseTimer += deltaTime) >= FleeChaseTimeout)
            {
                sm.ChangeState(sm.Wander);
                return;
            }
        }
        else
        {
            chaseTimer = 0f;
        }

        Vector3 targetPos = target.TF.position;
        float dx = targetPos.x - sm.CachedPosition.x;
        float dy = targetPos.y - sm.CachedPosition.y;
        float distSq = dx * dx + dy * dy;

        if (distSq > sm.VisionRadiusSq * 1.2f)
        {
            sm.ChangeState(sm.Wander);
            return;
        }

        if (distSq < MinAttackDistanceSq)
        {
            float currentDist = Mathf.Sqrt(distSq);
            Vector3 retreatDir = new Vector3(-dx, -dy, 0f).normalized;
            Vector3 retreatTarget = sm.CachedPosition + retreatDir * (MinAttackDistance - currentDist + 0.5f);
            
            if (sm.Map != null)
            {
                retreatTarget = sm.Map.ClampToMap(retreatTarget);
                if (!sm.Map.IsBlockedWorld(retreatTarget))
                    sm.MoveToward(retreatTarget, sm.GetCurrentSpeed() + ChaseSpeedBonus, deltaTime);
            }
            else
            {
                sm.MoveToward(retreatTarget, sm.GetCurrentSpeed() + ChaseSpeedBonus, deltaTime);
            }
            return;
        }

        if ((repathTimer -= deltaTime) <= 0f)
        {
            repathTimer = RepathInterval;
            BuildPathToTarget(sm);
        }

        sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed() + ChaseSpeedBonus, deltaTime);
    }

    public void Exit(CharacterStateMachine sm) => target = null;

    private void BuildPathToTarget(CharacterStateMachine sm)
    {
        if (target == null) return;
        pathIndex = 0;
        if (sm.Pathfinder != null)
            sm.Pathfinder.FindPath(sm.CachedPosition, target.TF.position, sm.PathBuffer);
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(target.TF.position);
        }
    }
}
