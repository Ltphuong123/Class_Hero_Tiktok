using UnityEngine;

/// <summary>
/// Chạy trốn xa kẻ địch trong 2 giây với tăng tốc tức thời.
/// Dùng pathfinder tránh tường. Sau 2 giây → WanderState.
/// </summary>
public class FleeState : ICharacterState
{
    private CharacterBase threat;
    private int pathIndex;
    private float fleeTimer;
    private float rescanTimer;
    private float lastPosX, lastPosY;
    private float stuckTimer;

    private const float FleeDuration = 2f;
    private const float RescanInterval = 0.3f;
    private const float FleeDistance = 12f;
    private const float StuckThreshold = 0.6f;
    private const float StuckMoveSq = 0.01f;

    public void SetThreat(CharacterBase t) => threat = t;

    public void Enter(CharacterStateMachine sm)
    {
        fleeTimer = 0f;
        rescanTimer = 0f;
        stuckTimer = 0f;
        lastPosX = sm.CachedPosition.x;
        lastPosY = sm.CachedPosition.y;

        // Luôn tăng tốc tức thời khi flee (bỏ qua cooldown)
        sm.FleeBoostTimer = sm.FleeSpeedDuration;

        CalculateFleeTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        fleeTimer += deltaTime;
        if (fleeTimer >= FleeDuration)
        {
            sm.ChangeState(sm.Wander);
            return;
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
                ForceRandomFleeTarget(sm);
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosX = cx;
            lastPosY = cy;
        }

        // Tính lại hướng chạy định kỳ (threat di chuyển)
        rescanTimer -= deltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = RescanInterval;
            CalculateFleeTarget(sm);
        }

        if (sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime))
            CalculateFleeTarget(sm);
    }

    public void Exit(CharacterStateMachine sm) => threat = null;

    private void ForceRandomFleeTarget(CharacterStateMachine sm)
    {
        Vector3 myPos = sm.CachedPosition;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(3f, FleeDistance);
            Vector3 candidate = new Vector3(
                myPos.x + Mathf.Cos(angle) * dist,
                myPos.y + Mathf.Sin(angle) * dist,
                myPos.z);

            if (sm.Map != null)
            {
                candidate = sm.Map.ClampToMap(candidate);
                if (sm.Map.IsWall(candidate)) continue;
            }

            pathIndex = 0;
            if (sm.Pathfinder != null)
            {
                float d = sm.Pathfinder.FindPath(myPos, candidate, sm.PathBuffer);
                if (d < float.MaxValue && sm.PathBuffer.Count > 0) return;
            }
            else
            {
                sm.PathBuffer.Clear();
                sm.PathBuffer.Add(candidate);
                return;
            }
        }
    }

    private void CalculateFleeTarget(CharacterStateMachine sm)
    {
        Vector3 myPos = sm.CachedPosition;

        // Hướng chạy = ngược hướng tất cả kẻ địch gần
        float fleeDirX = 0f, fleeDirY = 0f;

        if (sm.CharMgr != null)
        {
            sm.CharMgr.GetNearbyCharacters(myPos, sm.VisionRadius, sm.NearbyCharacters);
            int count = sm.NearbyCharacters.Count;
            for (int i = 0; i < count; i++)
            {
                CharacterBase other = sm.NearbyCharacters[i];
                if (other == sm.Owner || other.CurrentHp <= 0f) continue;

                float dx = myPos.x - other.Position.x;
                float dy = myPos.y - other.Position.y;
                float distSq = dx * dx + dy * dy;
                if (distSq < 0.01f) continue;

                float invDist = 1f / Mathf.Sqrt(distSq);
                fleeDirX += dx * invDist;
                fleeDirY += dy * invDist;
            }
        }

        // Fallback: dùng threat hoặc random
        float mag = Mathf.Sqrt(fleeDirX * fleeDirX + fleeDirY * fleeDirY);
        if (mag < 0.01f)
        {
            if (threat != null && threat.CurrentHp > 0f && threat.gameObject.activeInHierarchy)
            {
                fleeDirX = myPos.x - threat.Position.x;
                fleeDirY = myPos.y - threat.Position.y;
                mag = Mathf.Sqrt(fleeDirX * fleeDirX + fleeDirY * fleeDirY);
            }
            if (mag < 0.01f)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                fleeDirX = Mathf.Cos(a); fleeDirY = Mathf.Sin(a);
                mag = 1f;
            }
        }
        fleeDirX /= mag;
        fleeDirY /= mag;

        // Tìm điểm đến bằng pathfinder — 8 hướng xoay 45°
        Vector3 bestCandidate = Vector3.zero;
        bool found = false;

        for (int attempt = 0; attempt < 8 && !found; attempt++)
        {
            float angle = attempt * 0.785398f;
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float rdx = fleeDirX * cos - fleeDirY * sin;
            float rdy = fleeDirX * sin + fleeDirY * cos;

            for (float t = 1f; t >= 0.3f; t -= 0.2f)
            {
                Vector3 candidate = new Vector3(
                    myPos.x + rdx * FleeDistance * t,
                    myPos.y + rdy * FleeDistance * t,
                    myPos.z);

                if (sm.Map != null)
                {
                    candidate = sm.Map.ClampToMap(candidate);
                    if (sm.Map.IsWall(candidate)) continue;
                }

                // Dùng pathfinder tìm đường tránh tường
                if (sm.Pathfinder != null)
                {
                    float dist = sm.Pathfinder.FindPath(myPos, candidate, sm.PathBuffer);
                    if (dist >= float.MaxValue || sm.PathBuffer.Count == 0) continue;
                }
                else
                {
                    sm.PathBuffer.Clear();
                    sm.PathBuffer.Add(candidate);
                }

                bestCandidate = candidate;
                found = true;
                break;
            }
        }

        if (!found)
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(myPos);
        }

        pathIndex = 0;
    }
}
