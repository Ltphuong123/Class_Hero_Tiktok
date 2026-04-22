using UnityEngine;

public class WanderState : ICharacterState
{
    private int pathIndex;
    private float rescanTimer;
    private float stuckTimer;
    private float lastPosX, lastPosY;

    private const float RescanInterval = 0.2f;
    private const float WanderRadius = 8f;
    private const float StuckThreshold = 1.0f;
    private const float StuckMoveSq = 0.01f;

    public void Enter(CharacterStateMachine sm)
    {
        rescanTimer = 0f;
        stuckTimer = 0f;
        Vector3 pos = sm.CachedPosition;
        lastPosX = pos.x;
        lastPosY = pos.y;
        PickNewWanderTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if (sm.Owner.IsKnockedBack) return;

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

        Vector3 pos = sm.CachedPosition;
        float dx = pos.x - lastPosX, dy = pos.y - lastPosY;
        
        if (dx * dx + dy * dy < StuckMoveSq)
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
            lastPosY = pos.y;
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
        MapManager map = sm.Map;
        GridPathfinder pathfinder = sm.Pathfinder;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(3f, WanderRadius);
            float dirX = Mathf.Cos(angle), dirY = Mathf.Sin(angle);
            Vector3 candidate = new Vector3(myX + dirX * dist, myY + dirY * dist, myPos.z);

            if (map != null)
            {
                candidate = map.ClampToMap(candidate);
                if (map.IsWall(candidate)) continue;
            }

            float dx = candidate.x - myX, dy = candidate.y - myY;
            if (dx * dx + dy * dy < 1f) continue;

            pathIndex = 0;
            if (pathfinder != null)
            {
                float pathDist = pathfinder.FindPath(myPos, candidate, sm.PathBuffer);
                if (pathDist < float.MaxValue && sm.PathBuffer.Count > 0) return;
            }
            else
            {
                sm.PathBuffer.Clear();
                sm.PathBuffer.Add(candidate);
                return;
            }
        }

        sm.PathBuffer.Clear();
        pathIndex = 0;
    }
}
