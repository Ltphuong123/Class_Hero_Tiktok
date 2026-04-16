using UnityEngine;

/// <summary>
/// State mặc định: di chuyển ngẫu nhiên, scan tìm kiếm hoặc enemy.
/// Khi gần rìa map, ưu tiên hướng về trung tâm.
/// </summary>
public class WanderState : ICharacterState
{
    private int pathIndex;
    private float rescanTimer;
    private float stuckTimer;
    private float lastPosX, lastPosY;

    private const float RescanInterval = 0.5f;
    private const float WanderRadius = 8f;
    private const float StuckThreshold = 1.0f;
    private const float StuckMoveSq = 0.01f;

    public void Enter(CharacterStateMachine sm)
    {
        rescanTimer = 0f;
        stuckTimer = 0f;
        lastPosX = sm.CachedPosition.x;
        lastPosY = sm.CachedPosition.y;
        PickNewWanderTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        rescanTimer -= deltaTime;
        if (rescanTimer <= 0f)
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

            Sword sword = sm.FindBestSword();
            if (sword != null)
            {
                sm.CollectSword.SetTargetSword(sword);
                sm.ChangeState(sm.CollectSword);
                return;
            }
        }

        // Stuck detection
        float cx = sm.CachedPosition.x, cy = sm.CachedPosition.y;
        float mdx = cx - lastPosX, mdy = cy - lastPosY;
        if (mdx * mdx + mdy * mdy < StuckMoveSq)
        {
            stuckTimer += deltaTime;
            if (stuckTimer >= StuckThreshold)
            {
                stuckTimer = 0f;
                PickNewWanderTarget(sm);
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosX = cx;
            lastPosY = cy;
        }

        // Path hết hoặc rỗng → pick mới
        if (pathIndex >= sm.PathBuffer.Count || sm.PathBuffer.Count == 0)
        {
            PickNewWanderTarget(sm);
            return;
        }

        if (sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime))
            PickNewWanderTarget(sm);
    }

    public void Exit(CharacterStateMachine sm) { }

    private void PickNewWanderTarget(CharacterStateMachine sm)
    {
        Vector3 myPos = sm.CachedPosition;
        float myX = myPos.x, myY = myPos.y;

        // Nếu gần rìa map → bias hướng về trung tâm
        float biasX = 0f, biasY = 0f;
        if (sm.Map != null)
        {
            Vector2 min = sm.Map.MapMin;
            Vector2 max = sm.Map.MapMax;
            float centerX = (min.x + max.x) * 0.5f;
            float centerY = (min.y + max.y) * 0.5f;
            float edgeMargin = WanderRadius;

            if (myX - min.x < edgeMargin || max.x - myX < edgeMargin ||
                myY - min.y < edgeMargin || max.y - myY < edgeMargin)
            {
                biasX = centerX - myX;
                biasY = centerY - myY;
                float bMag = Mathf.Sqrt(biasX * biasX + biasY * biasY);
                if (bMag > 0.1f) { biasX /= bMag; biasY /= bMag; }
            }
        }

        for (int attempt = 0; attempt < 15; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dirX = Mathf.Cos(angle);
            float dirY = Mathf.Sin(angle);

            // Pha bias hướng trung tâm nếu gần rìa
            if (biasX != 0f || biasY != 0f)
            {
                dirX = dirX * 0.5f + biasX * 0.5f;
                dirY = dirY * 0.5f + biasY * 0.5f;
                float dMag = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (dMag > 0f) { dirX /= dMag; dirY /= dMag; }
            }

            float dist = Random.Range(3f, WanderRadius);
            Vector3 candidate = new Vector3(
                myX + dirX * dist,
                myY + dirY * dist,
                myPos.z
            );

            if (sm.Map != null)
            {
                candidate = sm.Map.ClampToMap(candidate);
                if (sm.Map.IsWall(candidate)) continue;
            }

            // Check khoảng cách sau clamp — phải đủ xa
            float cdx = candidate.x - myX;
            float cdy = candidate.y - myY;
            if (cdx * cdx + cdy * cdy < 1f) continue;

            pathIndex = 0;
            if (sm.Pathfinder != null)
            {
                float pathDist = sm.Pathfinder.FindPath(myPos, candidate, sm.PathBuffer);
                if (pathDist >= float.MaxValue || sm.PathBuffer.Count == 0) continue;

                // Check waypoint cuối phải khác vị trí hiện tại
                Vector3 last = sm.PathBuffer[sm.PathBuffer.Count - 1];
                float ldx = last.x - myX;
                float ldy = last.y - myY;
                if (ldx * ldx + ldy * ldy < 0.5f) continue;
            }
            else
            {
                sm.PathBuffer.Clear();
                sm.PathBuffer.Add(candidate);
            }
            return;
        }

        // Fallback: đi về hướng trung tâm map
        if (sm.Map != null)
        {
            Vector2 min = sm.Map.MapMin;
            Vector2 max = sm.Map.MapMax;
            float cx = (min.x + max.x) * 0.5f;
            float cy = (min.y + max.y) * 0.5f;

            // Đi 1 đoạn về trung tâm
            float toCX = cx - myX, toCY = cy - myY;
            float toMag = Mathf.Sqrt(toCX * toCX + toCY * toCY);
            if (toMag > 1f)
            {
                float step = Mathf.Min(5f, toMag);
                Vector3 fallback = new Vector3(
                    myX + toCX / toMag * step,
                    myY + toCY / toMag * step,
                    myPos.z
                );
                fallback = sm.Map.ClampToMap(fallback);

                if (!sm.Map.IsWall(fallback))
                {
                    pathIndex = 0;
                    if (sm.Pathfinder != null)
                    {
                        float d = sm.Pathfinder.FindPath(myPos, fallback, sm.PathBuffer);
                        if (d < float.MaxValue && sm.PathBuffer.Count > 0) return;
                    }
                    else
                    {
                        sm.PathBuffer.Clear();
                        sm.PathBuffer.Add(fallback);
                        return;
                    }
                }
            }
        }

        sm.PathBuffer.Clear();
        pathIndex = 0;
    }
}
