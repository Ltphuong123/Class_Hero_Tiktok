using UnityEngine;

public class AttackState : ICharacterState
{
    private CharacterBase target;
    private int pathIndex;
    private float repathTimer;
    private float chaseTimer;

    private const float RepathInterval = 0.5f;
    private const float FleeChaseTimeout = 2f;

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

        if ((repathTimer -= deltaTime) <= 0f)
        {
            repathTimer = RepathInterval;
            BuildPathToTarget(sm);
        }

        sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime);
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
