using UnityEngine;

/// <summary>
/// Đi nhặt kiếm rơi có path distance ngắn nhất trong tầm nhìn.
/// Nếu hết kiếm → Wander. Nếu thấy enemy yếu hơn + có kiếm → Attack.
/// </summary>
public class CollectSwordState : ICharacterState
{
    private Sword targetSword;
    private int pathIndex;
    private float rescanTimer;
    private float retargetTimer;

    private const float RescanInterval = 0.5f;
    private const float RetargetInterval = 1.5f;

    public void SetTargetSword(Sword sword) => targetSword = sword;

    public void Enter(CharacterStateMachine sm)
    {
        rescanTimer = 0f;
        retargetTimer = RetargetInterval;
        pathIndex = 0;

        if (targetSword != null && targetSword.State == SwordState.Dropped)
        {
            BuildPathToSword(sm);
        }
        else
        {
            // Không có target → tìm mới
            targetSword = sm.FindBestSword();
            if (targetSword != null)
                BuildPathToSword(sm);
        }
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        // Scan enemy yếu hơn (nếu có kiếm)
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

        // Kiểm tra target còn hợp lệ
        if (targetSword == null || targetSword.State != SwordState.Dropped || !targetSword.IsActive)
        {
            targetSword = sm.FindBestSword();
            if (targetSword == null)
            {
                sm.ChangeState(sm.Wander);
                return;
            }
            BuildPathToSword(sm);
        }

        // Định kỳ tìm kiếm tốt hơn
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

        // Di chuyển theo path
        bool arrived = sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime);
        if (arrived)
        {
            // Đến nơi nhưng kiếm có thể đã bị nhặt → tìm lại
            targetSword = sm.FindBestSword();
            if (targetSword != null)
                BuildPathToSword(sm);
            else
                sm.ChangeState(sm.Wander);
        }
    }

    public void Exit(CharacterStateMachine sm)
    {
        targetSword = null;
    }

    private void BuildPathToSword(CharacterStateMachine sm)
    {
        pathIndex = 0;
        if (sm.Pathfinder != null)
        {
            sm.Pathfinder.FindPath(sm.Owner.Position, targetSword.Position, sm.PathBuffer);
        }
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(targetSword.Position);
        }
    }
}
