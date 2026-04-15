using UnityEngine;

/// <summary>
/// Chạy trốn khi không có kiếm và bị tấn công.
/// Hướng chạy: xa khỏi threat + hướng về kiếm rơi gần nhất.
/// Tăng tốc đột ngột trong thời gian ngắn (có cooldown).
/// Khi nhặt được kiếm → CollectSword hoặc Attack.
/// </summary>
public class FleeState : ICharacterState
{
    private CharacterBase threat;
    private Vector3 fleeTarget;
    private int pathIndex;
    private float rescanTimer;

    private const float RescanInterval = 0.4f;
    private const float FleeDistance = 10f;

    public void SetThreat(CharacterBase t) => threat = t;

    public void Enter(CharacterStateMachine sm)
    {
        rescanTimer = 0f;
        sm.TryActivateFleeBoost();
        CalculateFleeTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        // Đã nhặt được kiếm → chuyển state
        if (sm.MySwordCount > 0)
        {
            // Có kiếm + threat vẫn gần → phản đòn
            if (threat != null && threat.CurrentHp > 0f && threat.gameObject.activeInHierarchy)
            {
                Vector3 myPos = sm.Owner.Position;
                float dx = threat.Position.x - myPos.x;
                float dy = threat.Position.y - myPos.y;
                float distSq = dx * dx + dy * dy;

                if (distSq <= sm.VisionRadius * sm.VisionRadius)
                {
                    sm.Attack.SetTarget(threat);
                    sm.ChangeState(sm.Attack);
                    return;
                }
            }

            // Threat xa hoặc chết → đi nhặt kiếm tiếp
            Sword sword = sm.FindBestSword();
            if (sword != null)
            {
                sm.CollectSword.SetTargetSword(sword);
                sm.ChangeState(sm.CollectSword);
            }
            else
            {
                sm.ChangeState(sm.Wander);
            }
            return;
        }

        // Scan lại hướng chạy
        rescanTimer -= deltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = RescanInterval;
            CalculateFleeTarget(sm);
        }

        // Di chuyển
        bool arrived = sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime);
        if (arrived)
        {
            // Đến nơi → tính lại hướng chạy
            CalculateFleeTarget(sm);
        }
    }

    public void Exit(CharacterStateMachine sm)
    {
        threat = null;
    }

    private void CalculateFleeTarget(CharacterStateMachine sm)
    {
        Vector3 myPos = sm.Owner.Position;

        // Hướng chạy xa khỏi threat
        Vector2 fleeDir;
        if (threat != null && threat.CurrentHp > 0f && threat.gameObject.activeInHierarchy)
        {
            fleeDir = new Vector2(myPos.x - threat.Position.x, myPos.y - threat.Position.y);
            if (fleeDir.sqrMagnitude < 0.01f)
                fleeDir = Random.insideUnitCircle.normalized;
            else
                fleeDir.Normalize();
        }
        else
        {
            fleeDir = Random.insideUnitCircle.normalized;
        }

        // Tìm kiếm gần nhất để hướng về
        Sword nearestSword = sm.FindBestSword();
        if (nearestSword != null)
        {
            Vector2 toSword = new Vector2(
                nearestSword.Position.x - myPos.x,
                nearestSword.Position.y - myPos.y
            );
            if (toSword.sqrMagnitude > 0.01f)
            {
                toSword.Normalize();
                // Weighted: 60% chạy xa threat, 40% hướng về kiếm
                fleeDir = (fleeDir * 0.6f + toSword * 0.4f).normalized;
            }
        }

        // Tính điểm đến
        Vector3 candidate = new Vector3(
            myPos.x + fleeDir.x * FleeDistance,
            myPos.y + fleeDir.y * FleeDistance,
            myPos.z
        );

        if (sm.Map != null)
        {
            candidate = sm.Map.ClampToMap(candidate);

            // Nếu trúng tường, thử giảm khoảng cách
            if (sm.Map.IsWall(candidate))
            {
                for (float t = 0.8f; t >= 0.2f; t -= 0.2f)
                {
                    Vector3 shorter = new Vector3(
                        myPos.x + fleeDir.x * FleeDistance * t,
                        myPos.y + fleeDir.y * FleeDistance * t,
                        myPos.z
                    );
                    shorter = sm.Map.ClampToMap(shorter);
                    if (!sm.Map.IsWall(shorter))
                    {
                        candidate = shorter;
                        break;
                    }
                }
            }
        }

        fleeTarget = candidate;
        pathIndex = 0;

        if (sm.Pathfinder != null)
        {
            sm.Pathfinder.FindPath(myPos, fleeTarget, sm.PathBuffer);
        }
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(fleeTarget);
        }
    }
}
