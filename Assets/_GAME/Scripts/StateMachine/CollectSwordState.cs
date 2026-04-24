using UnityEngine;

public class CollectSwordState : ICharacterState
{
    private Sword targetSword;
    private int pathIndex;
    private float rescanTimer;
    private float retargetTimer;

    private const float RescanInterval = 0.2f;
    private const float RetargetInterval = 1.5f;
    private const float PickupRadiusSq = 0.64f;

    public void SetTargetSword(Sword sword) => targetSword = sword;

    public void Enter(CharacterStateMachine sm)
    {
        rescanTimer = 0f;
        retargetTimer = RetargetInterval;
        pathIndex = 0;

        // Nếu đã đủ kiếm, chuyển sang Wander
        if (sm.Owner.IsSwordFull)
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

        // Nếu đã đủ kiếm, chuyển sang Wander hoặc Attack
        if (sm.Owner.IsSwordFull)
        {
            if (sm.MySwordCount > 0)
            {
                CharacterBase target = sm.FindWeakerTarget();
                if (target != null)
                {
                    sm.Attack.SetTarget(target);
                    sm.ChangeState(sm.Attack);
                    return;
                }
            }
            sm.ChangeState(sm.Wander);
            return;
        }

        if ((rescanTimer -= deltaTime) <= 0f)
        {
            rescanTimer = RescanInterval;
            
            if (sm.MySwordCount > 0)
            {
                CharacterBase target = sm.FindWeakerTarget();
                if (target != null)
                {
                    sm.Attack.SetTarget(target);
                    sm.ChangeState(sm.Attack);
                    return;
                }
            }
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
        float dy = targetSword.TF.position.y - sm.CachedPosition.y;
        
        if (dx * dx + dy * dy <= PickupRadiusSq)
        {
            if (targetSword.Collect(sm.Owner))
            {
                CharacterBase target = sm.FindWeakerTarget();
                if (target != null)
                {
                    sm.Attack.SetTarget(target);
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
