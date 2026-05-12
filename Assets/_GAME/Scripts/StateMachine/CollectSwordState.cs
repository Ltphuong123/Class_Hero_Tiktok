using UnityEngine;

public class CollectSwordState : ICharacterState
{
    private Sword targetSword;
    private int pathIndex;
    private float retargetTimer;

    private const float RetargetInterval = 1.5f;
    private const float PickupRadiusSq = 0.64f;

    public void SetTargetSword(Sword sword) => targetSword = sword;

    public void Enter(CharacterStateMachine sm)
    {
        retargetTimer = RetargetInterval;
        pathIndex = 0;

        if (sm.Owner.IsSwordFull || sm.Owner.SwordQueue > 0)
        {
            sm.ChangeState(sm.Wander);
            return;
        }

        if (targetSword == null || targetSword.State != SwordState.Dropped)
            targetSword = sm.FindBestSword();

        if (targetSword != null)
            BuildPathToSword(sm);
        else
            sm.ChangeState(sm.Wander);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if (sm.Owner.IsKnockedBack) return;

        if (sm.Owner.IsSwordFull || sm.Owner.SwordQueue > 0)
        {
            sm.ChangeState(sm.Wander);
            return;
        }

        if (targetSword == null || targetSword.State != SwordState.Dropped || !targetSword.gameObject.activeSelf)
        {
            targetSword = sm.FindBestSword();
            if (targetSword == null)
            {
                sm.ChangeState(sm.Wander);
                return;
            }
            BuildPathToSword(sm);
        }

        float dx = targetSword.TF.position.x - sm.CachedPosition.x;
        float dz = targetSword.TF.position.z - sm.CachedPosition.z;

        if (dx * dx + dz * dz <= PickupRadiusSq)
        {
            if (targetSword.Collect(sm.Owner))
            {
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

        if ((retargetTimer -= deltaTime) <= 0f)
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
            if (targetSword != null)
                BuildPathToSword(sm);
            else
                sm.ChangeState(sm.Wander);
        }
    }

    public void Exit(CharacterStateMachine sm) => targetSword = null;

    private void BuildPathToSword(CharacterStateMachine sm)
    {
        pathIndex = 0;
        if (sm.Pathfinder != null)
            sm.Pathfinder.FindPath(sm.CachedPosition, targetSword.TF.position, sm.PathBuffer);
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(targetSword.TF.position);
        }
    }
}
