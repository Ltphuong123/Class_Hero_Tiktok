using UnityEngine;

public class WanderState : ICharacterState
{
    private int pathIndex;
    private float rescanTimer;
    private float stuckTimer;
    private float lastPosX, lastPosY;

    private const float RescanInterval = 0.2f;  // Giảm từ 0.5s → 0.2s để phản ứng nhanh hơn
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
                float bMagSq = biasX * biasX + biasY * biasY;
                if (bMagSq > 0.01f)
                {
                    float invMag = 1f / Mathf.Sqrt(bMagSq);
                    biasX *= invMag;
                    biasY *= invMag;
                }
            }
        }

        // Reduced from 15 to 10 attempts for better performance
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dirX = Mathf.Cos(angle);
            float dirY = Mathf.Sin(angle);

            if (biasX != 0f || biasY != 0f)
            {
                dirX = dirX * 0.5f + biasX * 0.5f;
                dirY = dirY * 0.5f + biasY * 0.5f;
                float dMagSq = dirX * dirX + dirY * dirY;
                if (dMagSq > 0f)
                {
                    float invMag = 1f / Mathf.Sqrt(dMagSq);
                    dirX *= invMag;
                    dirY *= invMag;
                }
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

            float cdx = candidate.x - myX;
            float cdy = candidate.y - myY;
            if (cdx * cdx + cdy * cdy < 1f) continue;

            pathIndex = 0;
            if (sm.Pathfinder != null)
            {
                float pathDist = sm.Pathfinder.FindPath(myPos, candidate, sm.PathBuffer);
                if (pathDist >= float.MaxValue || sm.PathBuffer.Count == 0) continue;

                Vector3 last = sm.PathBuffer[sm.PathBuffer.Count - 1];
                float ldx = last.x - myX;
                float ldy = last.y - myY;
                if (ldx * ldx + ldy * ldy < 0.25f) continue; // 0.5f * 0.5f pre-calculated
            }
            else
            {
                sm.PathBuffer.Clear();
                sm.PathBuffer.Add(candidate);
            }
            return;
        }

        // Fallback: move toward center
        if (sm.Map != null)
        {
            Vector2 min = sm.Map.MapMin;
            Vector2 max = sm.Map.MapMax;
            float cx = (min.x + max.x) * 0.5f;
            float cy = (min.y + max.y) * 0.5f;

            float toCX = cx - myX, toCY = cy - myY;
            float toMagSq = toCX * toCX + toCY * toCY;
            if (toMagSq > 1f)
            {
                float toMag = Mathf.Sqrt(toMagSq);
                float step = Mathf.Min(5f, toMag);
                float invMag = step / toMag;
                Vector3 fallback = new Vector3(
                    myX + toCX * invMag,
                    myY + toCY * invMag,
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
