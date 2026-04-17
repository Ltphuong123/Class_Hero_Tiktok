using UnityEngine;

public class AttackState : ICharacterState
{
    private CharacterBase target;
    private int pathIndex;
    private float repathTimer;
    private float chaseTimer;
    private Vector3 lastTargetPos;

    private const float RepathInterval = 0.5f;  // Increased from 0.3f for better performance
    private const float TargetMovedThresholdSq = 2.25f;  // 1.5f * 1.5f (pre-calculated)
    private const float ChaseDuration = 5f;

    public void SetTarget(CharacterBase t) => target = t;

    public void Enter(CharacterStateMachine sm)
    {
        repathTimer = 0f;
        chaseTimer = 0f;
        pathIndex = 0;
        lastTargetPos = Vector3.zero;
        if (target != null) BuildPathToTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if (sm.MySwordCount <= 0)
        {
            if (target != null) sm.Flee.SetThreat(target);
            sm.ChangeState(sm.Flee);
            return;
        }

        if (target == null || target.CurrentHp <= 0f || !target.gameObject.activeInHierarchy)
        {
            target = sm.FindWeakerTarget();
            if (target != null)
            {
                chaseTimer = 0f;
                BuildPathToTarget(sm);
            }
            else { GiveUpChase(sm); return; }
        }

        Vector3 myPos = sm.CachedPosition;
        Vector3 targetPos = target.Position;
        float dx = targetPos.x - myPos.x;
        float dy = targetPos.y - myPos.y;
        float distSq = dx * dx + dy * dy;

        if (distSq > sm.VisionRadiusSq * 1.2f)
        {
            GiveUpChase(sm);
            return;
        }

        chaseTimer += deltaTime;
        if (chaseTimer >= ChaseDuration)
        {
            GiveUpChase(sm);
            return;
        }

        repathTimer -= deltaTime;
        float mdx = targetPos.x - lastTargetPos.x;
        float mdy = targetPos.y - lastTargetPos.y;
        if (repathTimer <= 0f || (mdx * mdx + mdy * mdy) > TargetMovedThresholdSq)
        {
            repathTimer = RepathInterval;
            BuildPathToTarget(sm);
        }

        sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime);
    }

    public void Exit(CharacterStateMachine sm) => target = null;

    private void GiveUpChase(CharacterStateMachine sm)
    {
        target = null;
        Sword sword = sm.FindBestSword();
        if (sword != null)
        {
            sm.CollectSword.SetTargetSword(sword);
            sm.ChangeState(sm.CollectSword);
        }
        else sm.ChangeState(sm.Wander);
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
