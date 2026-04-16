using UnityEngine;

/// <summary>
/// Đuổi theo target yếu hơn bằng pathfinding.
/// Separation trong CharacterStateMachine tự đẩy ra khi quá gần.
/// Đuổi tối đa 3 giây, không kịp → nhặt kiếm.
/// </summary>
public class AttackState : ICharacterState
{
    private CharacterBase target;
    private int pathIndex;
    private float repathTimer;
    private float chaseTimer;
    private Vector3 lastTargetPos;

    private const float RepathInterval = 0.3f;
    private const float TargetMovedThresholdSq = 1.5f * 1.5f;
    private const float ChaseDuration = 3f;

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

        // Ngoài tầm nhìn → bỏ
        if (distSq > sm.VisionRadiusSq * 1.2f)
        {
            GiveUpChase(sm);
            return;
        }

        // Đếm thời gian đuổi
        chaseTimer += deltaTime;
        if (chaseTimer >= ChaseDuration)
        {
            GiveUpChase(sm);
            return;
        }

        // Repath khi cần
        repathTimer -= deltaTime;
        float mdx = targetPos.x - lastTargetPos.x;
        float mdy = targetPos.y - lastTargetPos.y;
        if (repathTimer <= 0f || (mdx * mdx + mdy * mdy) > TargetMovedThresholdSq)
        {
            repathTimer = RepathInterval;
            BuildPathToTarget(sm);
        }

        // Di chuyển về phía target — separation tự đẩy ra khi quá gần
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
