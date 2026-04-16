using UnityEngine;

public class CollectSwordState : ICharacterState
{
    private Sword targetSword;
    private int pathIndex;
    private float rescanTimer;
    private float retargetTimer;

    private const float RescanInterval = 0.5f;
    private const float RetargetInterval = 1.5f;
    private const float PickupRadiusSq = 0.8f * 0.8f;

    public void SetTargetSword(Sword sword) => targetSword = sword;

    public void Enter(CharacterStateMachine sm)
    {
        rescanTimer = 0f;
        retargetTimer = RetargetInterval;
        pathIndex = 0;

        if (targetSword != null && targetSword.State == SwordState.Dropped)
        {
            BuildPathToSword(sm);
            return;
        }

        targetSword = sm.FindBestSword();
        if (targetSword != null)
        {
            BuildPathToSword(sm);
            return;
        }

        sm.ChangeState(sm.Wander);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        rescanTimer -= deltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = RescanInterval;
            if (sm.MySwordCount > 0)
            {
                CharacterBase weakTarget = sm.FindWeakerTarget();
                if (weakTarget != null)
                {
                    sm.Attack.SetTarget(weakTarget);
                    sm.ChangeState(sm.Attack);
                    return;
                }
            }
        }

        if (targetSword == null || targetSword.State != SwordState.Dropped || !targetSword.IsActive)
        {
            targetSword = sm.FindBestSword();
            if (targetSword == null) { sm.ChangeState(sm.Wander); return; }
            BuildPathToSword(sm);
        }

        float dx = targetSword.Position.x - sm.CachedPosition.x;
        float dy = targetSword.Position.y - sm.CachedPosition.y;
        if (dx * dx + dy * dy <= PickupRadiusSq)
        {
            if (targetSword.Collect(sm.Owner))
            {
                // Successfully collected! Now find next target
                CharacterBase weakTarget = sm.FindWeakerTarget();
                if (weakTarget != null)
                {
                    sm.Attack.SetTarget(weakTarget);
                    sm.ChangeState(sm.Attack);
                    return;
                }

                targetSword = sm.FindBestSword();
                if (targetSword != null)
                {
                    BuildPathToSword(sm);
                    return;
                }

                sm.ChangeState(sm.Wander);
                return;
            }
        }

        retargetTimer -= deltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = RetargetInterval;
            Sword better = sm.FindBestSword();
            if (better != null && better != targetSword)
            {
                targetSword = better;
                BuildPathToSword(sm);
            }
        }

        if (sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime))
        {
            targetSword = sm.FindBestSword();
            if (targetSword != null) BuildPathToSword(sm);
            else sm.ChangeState(sm.Wander);
        }
    }

    public void Exit(CharacterStateMachine sm) => targetSword = null;

    private void BuildPathToSword(CharacterStateMachine sm)
    {
        pathIndex = 0;
        if (sm.Pathfinder != null)
            sm.Pathfinder.FindPath(sm.CachedPosition, targetSword.Position, sm.PathBuffer);
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(targetSword.Position);
        }
    }
}
