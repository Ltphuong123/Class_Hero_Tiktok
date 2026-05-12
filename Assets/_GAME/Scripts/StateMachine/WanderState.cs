using UnityEngine;

public class WanderState : ICharacterState
{
    private int pathIndex;
    private float rescanTimer;
    private float stuckTimer;
    private float lastPosX, lastPosZ;

    private const float RescanInterval = 0.2f;
    private const float WanderRadius   = 18f;
    private const float StuckThreshold = 1.0f;
    private const float StuckMoveSq    = 0.01f;
    private const float MinPickDist    = 3f;
    private const int   MaxWallThickness = 7;

    private const float ZoneMinX = -15f;
    private const float ZoneMaxX =  15f;
    private const float ZoneMinZ = -15f;
    private const float ZoneMaxZ =  15f;

    public void Enter(CharacterStateMachine sm)
    {
        rescanTimer = 0f;
        stuckTimer  = 0f;
        Vector3 pos = sm.CachedPosition;
        lastPosX = pos.x;
        lastPosZ = pos.z;
        PickNewWanderTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if (sm.Owner.IsKnockedBack) return;

        rescanTimer -= deltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = RescanInterval;

            CharacterBase target = sm.FindWeakerTarget();
            if (target != null)
            {
                sm.Attack.SetTarget(target);
                sm.ChangeState(sm.Attack);
                return;
            }

            if (!sm.Owner.IsSwordFull && sm.Owner.SwordQueue == 0)
            {
                Sword sword = sm.FindBestSword();
                if (sword != null)
                {
                    sm.CollectSword.SetTargetSword(sword);
                    sm.ChangeState(sm.CollectSword);
                    return;
                }
            }
        }

        Vector3 pos = sm.CachedPosition;
        float dx = pos.x - lastPosX, dz = pos.z - lastPosZ;

        if (dx * dx + dz * dz < StuckMoveSq)
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
            lastPosX = pos.x;
            lastPosZ = pos.z;
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
        Vector3 myPos  = sm.CachedPosition;
        float myX = myPos.x, myZ = myPos.z;
        MapManager map = sm.Map;
        GridPathfinder pathfinder = sm.Pathfinder;
        float cellSize = map != null ? map.CellSize : 1f;

        bool insideZone = myX >= ZoneMinX && myX <= ZoneMaxX &&
                          myZ >= ZoneMinZ && myZ <= ZoneMaxZ;

        Vector3 candidate = insideZone
            ? ScanInsideZone(myX, myZ, cellSize, map)
            : ScanTowardZone(myX, myZ, cellSize, map);

        if (candidate == Vector3.zero)
        {
            sm.PathBuffer.Clear();
            pathIndex = 0;
            return;
        }

        pathIndex = 0;
        if (pathfinder != null)
        {
            float dist = pathfinder.FindPath(myPos, candidate, sm.PathBuffer);
            if (dist >= float.MaxValue || sm.PathBuffer.Count == 0)
            {
                sm.PathBuffer.Clear();
                pathIndex = 0;
            }
        }
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(candidate);
        }
    }

    private Vector3 ScanInsideZone(float myX, float myZ, float cellSize, MapManager map)
    {
        float startAngle = Random.Range(0f, Mathf.PI * 2f);
        float maxScan    = WanderRadius + MaxWallThickness * cellSize;

        for (int dir = 0; dir < 8; dir++)
        {
            float angle = startAngle + dir * (Mathf.PI * 0.25f);
            float dirX  = Mathf.Cos(angle);
            float dirZ  = Mathf.Sin(angle);

            for (float d = cellSize; d <= maxScan; d += cellSize)
            {
                float cx = Mathf.Clamp(myX + dirX * d, ZoneMinX, ZoneMaxX);
                float cz = Mathf.Clamp(myZ + dirZ * d, ZoneMinZ, ZoneMaxZ);
                Vector3 p = new Vector3(cx, 0f, cz);
                if (map != null) p = map.ClampToMap(p);

                if (map == null || !map.IsWall(p))
                {
                    float ddx = cx - myX, ddz = cz - myZ;
                    if (ddx * ddx + ddz * ddz >= MinPickDist * MinPickDist)
                        return p;
                }
            }
        }

        return Vector3.zero;
    }

    private Vector3 ScanTowardZone(float myX, float myZ, float cellSize, MapManager map)
    {
        float toCX  = -myX, toCZ = -myZ;
        float mag   = Mathf.Sqrt(toCX * toCX + toCZ * toCZ);
        float angle = mag > 0.01f ? Mathf.Atan2(toCZ, toCX) : 0f;
        float maxScan = mag + MaxWallThickness * cellSize;

        float dirX = Mathf.Cos(angle);
        float dirZ = Mathf.Sin(angle);

        for (float d = cellSize; d <= maxScan; d += cellSize)
        {
            float cx = Mathf.Clamp(myX + dirX * d, ZoneMinX, ZoneMaxX);
            float cz = Mathf.Clamp(myZ + dirZ * d, ZoneMinZ, ZoneMaxZ);
            Vector3 p = new Vector3(cx, 0f, cz);
            if (map != null) p = map.ClampToMap(p);

            bool inZone = cx >= ZoneMinX && cx <= ZoneMaxX &&
                          cz >= ZoneMinZ && cz <= ZoneMaxZ;

            if (inZone && (map == null || !map.IsWall(p)))
                return p;
        }

        return Vector3.zero;
    }
}
