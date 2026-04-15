using UnityEngine;

/// <summary>
/// State mặc định: di chuyển ngẫu nhiên khi không có kiếm và không có enemy yếu hơn trong tầm nhìn.
/// Liên tục scan tìm kiếm hoặc mục tiêu tấn công.
/// </summary>
public class WanderState : ICharacterState
{
    private Vector3 wanderTarget;
    private int pathIndex;
    private float rescanTimer;

    private const float RescanInterval = 0.5f;
    private const float WanderRadius = 8f;
    private const float ArriveThreshold = 0.5f;

    public void Enter(CharacterStateMachine sm)
    {
        PickNewWanderTarget(sm);
        rescanTimer = 0f;
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        // Scan định kỳ tìm kiếm hoặc enemy
        rescanTimer -= deltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = RescanInterval;

            // Ưu tiên: tìm enemy yếu hơn trước (nếu có kiếm)
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

            // Tìm kiếm rơi
            Sword sword = sm.FindBestSword();
            if (sword != null)
            {
                sm.CollectSword.SetTargetSword(sword);
                sm.ChangeState(sm.CollectSword);
                return;
            }
        }

        // Di chuyển theo path
        bool arrived = sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime);
        if (arrived)
        {
            PickNewWanderTarget(sm);
        }
    }

    public void Exit(CharacterStateMachine sm) { }

    private void PickNewWanderTarget(CharacterStateMachine sm)
    {
        Vector3 myPos = sm.Owner.Position;

        // Thử tìm điểm ngẫu nhiên không trúng tường
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(3f, WanderRadius);
            Vector3 candidate = new Vector3(
                myPos.x + Mathf.Cos(angle) * dist,
                myPos.y + Mathf.Sin(angle) * dist,
                myPos.z
            );

            if (sm.Map != null)
            {
                candidate = sm.Map.ClampToMap(candidate);
                if (sm.Map.IsWall(candidate)) continue;
            }

            wanderTarget = candidate;

            // Tìm đường
            if (sm.Pathfinder != null)
            {
                sm.Pathfinder.FindPath(myPos, wanderTarget, sm.PathBuffer);
                pathIndex = 0;
            }
            else
            {
                sm.PathBuffer.Clear();
                sm.PathBuffer.Add(wanderTarget);
                pathIndex = 0;
            }
            return;
        }

        // Fallback: đứng yên 1 frame rồi thử lại
        sm.PathBuffer.Clear();
        sm.PathBuffer.Add(myPos);
        pathIndex = 0;
    }
}
