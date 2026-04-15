using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scoring-based AI brain component. Attaches to a CharacterBase GameObject
/// and makes autonomous decisions across four states (Wander, Collect, Chase, Flee).
/// Evaluation is called by AIManager on a staggered schedule; movement runs every frame.
/// </summary>
public class AIBrain : MonoBehaviour
{
    // ── Serialized tuning fields ──
    [Header("Perception")]
    [SerializeField] private float enemyPerceptionRadius = 10f;
    [SerializeField] private float itemPerceptionRadius = 14f;
    [SerializeField] private float wanderChangeInterval = 2f;
    [SerializeField] private float wallAvoidDistance = 1.5f;
    [SerializeField] private LayerMask wallMask;

    // ── Internal state ──
    private AIState currentState = AIState.Wander;
    private CharacterBase chaseTarget;
    private ICollectibleItem collectTarget;
    private CharacterBase fleeTarget;
    private Vector2 wanderDirection;
    private float wanderTimer;
    private readonly float[] scores = new float[4];
    private readonly List<CharacterBase> nearbyEnemies = new();
    private readonly List<ICollectibleItem> nearbyItems = new();
    private bool hasLoggedManagerWarning;

    // ── Movement smoothing ──
    private Vector2 currentDirection;
    private Vector2 targetDirection;
    private const float DirectionSmoothSpeed = 8f;

    // ── State change hysteresis ──
    private float stateChangeTimer;
    private const float MinStateHoldTime = 0.5f; // don't switch state too fast

    // ── Pathfinding ──
    private List<Vector3> currentPath = new();
    private int pathIndex;
    private float pathRefreshTimer;
    private const float PathRefreshInterval = 0.5f; // recalculate path every 0.5s
    private const float WaypointReachDist = 0.5f; // distance to consider waypoint reached

    // ── Cached references ──
    private CharacterBase owner;

    // ── Read-only properties ──
    public AIState CurrentState => currentState;
    public CharacterBase Owner => owner;
    public float EnemyPerceptionRadius => enemyPerceptionRadius;
    public float ItemPerceptionRadius => itemPerceptionRadius;

    public void SetEnemyPerceptionRadius(float r) => enemyPerceptionRadius = Mathf.Max(0f, r);
    public void SetItemPerceptionRadius(float r) => itemPerceptionRadius = Mathf.Max(0f, r);

    public object CurrentTarget
    {
        get
        {
            switch (currentState)
            {
                case AIState.Chase: return chaseTarget;
                case AIState.Collect: return collectTarget;
                case AIState.Flee: return fleeTarget;
                default: return null;
            }
        }
    }

    // ── Lifecycle ──

    private void Awake()
    {
        owner = GetComponent<CharacterBase>();
        wanderDirection = Random.insideUnitCircle.normalized;
        wanderTimer = wanderChangeInterval;
    }

    private void OnEnable()
    {
        if (AIManager.Instance != null)
            AIManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (AIManager.Instance != null)
            AIManager.Instance.Deregister(this);
    }

    // ── Evaluate — called by AIManager during staggered evaluation ──

    public void Evaluate()
    {
        if (owner == null) return;

        // Hysteresis: don't switch state too rapidly
        stateChangeTimer -= Time.deltaTime;

        // Check manager availability
        CharacterManager charMgr = CharacterManager.Instance;
        ItemManager itemMgr = ItemManager.Instance;

        if (charMgr == null || itemMgr == null)
        {
            currentState = AIState.Wander;
            if (!hasLoggedManagerWarning)
            {
                Debug.LogWarning("[AIBrain] CharacterManager or ItemManager is null. Defaulting to Wander.");
                hasLoggedManagerWarning = true;
            }
            return;
        }

        // Query nearby enemies
        charMgr.GetNearbyCharacters(owner.Position, enemyPerceptionRadius, nearbyEnemies);

        // Exclude self from results
        for (int i = nearbyEnemies.Count - 1; i >= 0; i--)
        {
            if (nearbyEnemies[i] == owner)
                nearbyEnemies.RemoveAt(i);
        }

        // Query items (1 lần duy nhất, filter sau)
        itemMgr.GetNearbyItems(owner.Position, itemPerceptionRadius, nearbyItems);

        // Tìm sword và item gần nhất bằng A* path distance
        GridPathfinder pf = MapManager.Instance != null ? MapManager.Instance.Pathfinder : null;

        Sword bestSword = null;
        float bestSwordDist = float.MaxValue;
        ICollectibleItem bestItem = null;
        float bestItemDist = float.MaxValue;

        for (int i = 0; i < nearbyItems.Count; i++)
        {
            ICollectibleItem item = nearbyItems[i];
            float dist = GetPathDistance(pf, owner.Position, item.Position);

            if (item is Sword sword && sword.State == SwordState.Dropped)
            {
                if (dist < bestSwordDist) { bestSwordDist = dist; bestSword = sword; }
            }
            if (dist < bestItemDist) { bestItemDist = dist; bestItem = item; }
        }

        bool hasSwordNearby = bestSword != null;
        bool hasItemNearby = bestItem != null;

        // Chọn collect target: cái nào path distance ngắn hơn
        ICollectibleItem bestCollectTarget;
        if (bestSword != null && bestItem != null && bestItem != (ICollectibleItem)bestSword)
            bestCollectTarget = bestSwordDist <= bestItemDist ? bestSword : bestItem;
        else
            bestCollectTarget = bestSword != null ? (ICollectibleItem)bestSword : bestItem;

        // Compute scores
        int ownSwords = owner.GetSwordOrbit() != null ? owner.GetSwordOrbit().SwordCount : 0;
        float ownHp = owner.CurrentHp;
        float maxHp = owner.MaxHp;

        // ── Bị tấn công? Ưu tiên chase kẻ tấn công mình ──
        CharacterBase attacker = owner.LastAttacker;

        // ── Tìm chase target ──
        CharacterBase bestChaseTarget = null;
        float bestChaseDist = float.MaxValue;

        // ── Tìm flee target: kẻ địch mạnh hơn + gần nhất (nguy hiểm nhất) ──
        CharacterBase bestFleeTarget = null;
        float bestFleeDist = float.MaxValue;

        // Nếu đang bị tấn công → ưu tiên chase kẻ đó (bất kể số kiếm)
        if (attacker != null && ownSwords > 0 && nearbyEnemies.Contains(attacker))
        {
            bestChaseTarget = attacker;
            bestChaseDist = GetPathDistance(pf, owner.Position, attacker.Position);
        }

        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            CharacterBase enemy = nearbyEnemies[i];
            int enemySwords = enemy.GetSwordOrbit() != null ? enemy.GetSwordOrbit().SwordCount : 0;

            // Nếu đã lock attacker → skip tìm chase target khác
            if (attacker == null || bestChaseTarget != attacker)
            {
                if (ownSwords > 0 && enemySwords < ownSwords)
                {
                    float dist = GetPathDistance(pf, owner.Position, enemy.Position);
                    if (dist < bestChaseDist)
                    {
                        bestChaseDist = dist;
                        bestChaseTarget = enemy;
                    }
                }
            }

            if (enemySwords > ownSwords && enemy != attacker)
            {
                float dist = GetPathDistance(pf, owner.Position, enemy.Position);
                if (dist < bestFleeDist)
                {
                    bestFleeDist = dist;
                    bestFleeTarget = enemy;
                }
            }
        }

        bool hasChaseTarget = bestChaseTarget != null;
        bool hasFleeTarget = bestFleeTarget != null;

        // ── Đang Chase và target vẫn hợp lệ? → bám theo liên tục, KHÔNG đổi target ──
        if (currentState == AIState.Chase && chaseTarget != null
            && chaseTarget.gameObject.activeInHierarchy && chaseTarget.CurrentHp > 0f)
        {
            // Mất hết kiếm → không thể tấn công, bỏ chase ngay
            if (ownSwords <= 0)
            {
                chaseTarget = null;
            }
            else
            {
                int targetSwords = chaseTarget.GetSwordOrbit() != null ? chaseTarget.GetSwordOrbit().SwordCount : 0;
                bool isAttacker = chaseTarget == attacker;
                bool isStronger = targetSwords >= ownSwords && !isAttacker;

                // Quá xa (>11 ô) → bỏ cuộc
                float chaseDist = GetPathDistance(pf, owner.Position, chaseTarget.Position);
                float maxChaseDistance = MapManager.Instance != null ? MapManager.Instance.CellSize * 11f : 55f;
                bool tooFar = chaseDist > maxChaseDistance;

                if (!isStronger && !tooFar)
                {
                    collectTarget = null;
                    fleeTarget = null;
                    return;
                }

                // Bỏ chase: target mạnh hơn hoặc quá xa
                chaseTarget = null;
            }
        }

        scores[0] = AIScoring.ComputeWanderScore(nearbyEnemies.Count, hasSwordNearby || hasItemNearby);
        scores[1] = AIScoring.ComputeCollectScore(hasSwordNearby, hasItemNearby, ownSwords, 8);
        // Bị tấn công → chase score cao hơn bình thường
        scores[2] = hasChaseTarget ? AIScoring.ComputeChaseScore(ownSwords, ownHp,
            bestChaseTarget.GetSwordOrbit() != null ? bestChaseTarget.GetSwordOrbit().SwordCount : 0,
            bestChaseTarget.CurrentHp, maxHp) : 0f;
        if (attacker != null && bestChaseTarget == attacker)
            scores[2] = Mathf.Max(scores[2], 0.9f); // ưu tiên cao, gần như chắc chắn chase
        scores[3] = hasFleeTarget ? AIScoring.ComputeFleeScore(ownSwords, ownHp,
            bestFleeTarget.GetSwordOrbit() != null ? bestFleeTarget.GetSwordOrbit().SwordCount : 0,
            bestFleeTarget.CurrentHp, maxHp) : 0f;

        // Select state (with hysteresis bonus for current state)
        if (stateChangeTimer > 0f)
            scores[(int)currentState] += 0.15f; // bonus to stay in current state

        AIState newState = AIScoring.SelectState(scores, currentState);
        if (newState != currentState)
            stateChangeTimer = MinStateHoldTime;
        currentState = newState;

        // Update target references based on new state
        switch (currentState)
        {
            case AIState.Chase:
                chaseTarget = bestChaseTarget;
                collectTarget = null;
                fleeTarget = null;
                break;
            case AIState.Collect:
                collectTarget = bestCollectTarget;
                chaseTarget = null;
                fleeTarget = null;
                break;
            case AIState.Flee:
                fleeTarget = bestFleeTarget;
                chaseTarget = null;
                collectTarget = null;
                break;
            default: // Wander
                chaseTarget = null;
                collectTarget = null;
                fleeTarget = null;
                break;
        }
    }

    /// <summary>
    /// Tính khoảng cách thực tế theo đường A* (chỉ distance, không tạo waypoint list).
    /// Fallback về Euclidean nếu không có pathfinder.
    /// </summary>
    private static float GetPathDistance(GridPathfinder pf, Vector3 from, Vector3 to)
    {
        if (pf == null)
            return Vector3.Distance(from, to);

        return pf.FindPathDistance(from, to);
    }

    // ── ExecuteMovement — called by AIManager every frame ──

    public void ExecuteMovement(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (owner == null) return;
        if (owner.IsKnockedBack) return; // đang bị đẩy, không di chuyển

        float moveSpeed = owner.MoveSpeed;
        targetDirection = Vector2.zero;

        // Refresh path timer
        pathRefreshTimer -= deltaTime;

        switch (currentState)
        {
            case AIState.Wander:
                wanderTimer -= deltaTime;
                if (wanderTimer <= 0f)
                {
                    float angle = Random.Range(-90f, 90f);
                    wanderDirection = RotateVector(wanderDirection, angle).normalized;
                    if (wanderDirection == Vector2.zero)
                        wanderDirection = Random.insideUnitCircle.normalized;
                    wanderTimer = wanderChangeInterval + Random.Range(-0.5f, 0.5f);
                }
                targetDirection = wanderDirection;
                currentPath.Clear();
                break;

            case AIState.Collect:
                if (collectTarget == null || !collectTarget.IsActive)
                {
                    Evaluate();
                    return;
                }
                targetDirection = GetPathDirection(collectTarget.Position, deltaTime);
                break;

            case AIState.Chase:
                if (chaseTarget == null)
                {
                    Evaluate();
                    return;
                }
                targetDirection = GetPathDirection(chaseTarget.Position, deltaTime);
                break;

            case AIState.Flee:
                if (fleeTarget == null)
                {
                    Evaluate();
                    return;
                }
                // Flee: pick a point away from threat, then pathfind to it
                Vector3 fleePoint = owner.Position + (owner.Position - fleeTarget.Position).normalized * enemyPerceptionRadius;
                MapManager fleeMap = MapManager.Instance;
                if (fleeMap != null) fleePoint = fleeMap.ClampToMap(fleePoint);
                targetDirection = GetPathDirection(fleePoint, deltaTime);
                break;
        }

        // Smooth direction transition (prevents jerky movement)
        currentDirection = Vector2.Lerp(currentDirection, targetDirection, DirectionSmoothSpeed * deltaTime);
        if (currentDirection.sqrMagnitude > 0.001f)
            currentDirection = currentDirection.normalized;

        // Steer away from map edges (soft boundary)
        currentDirection = SteerAwayFromBounds((Vector2)owner.Position, currentDirection);

        // Apply wall avoidance (raycast fallback)
        Vector2 finalDir = AdjustForWalls((Vector2)owner.Position, currentDirection);

        // Apply movement
        owner.transform.position += (Vector3)(finalDir * moveSpeed * deltaTime);
    }

    /// <summary>
    /// Get movement direction toward a target using A* pathfinding.
    /// Falls back to direct direction if pathfinder is unavailable.
    /// </summary>
    private Vector2 GetPathDirection(Vector3 targetPos, float deltaTime)
    {
        MapManager map = MapManager.Instance;
        GridPathfinder pathfinder = map != null ? map.Pathfinder : null;

        // If no pathfinder, fall back to direct movement
        if (pathfinder == null)
            return ((Vector2)(targetPos - owner.Position)).normalized;

        // Refresh path periodically or if we have no path
        if (currentPath.Count == 0 || pathRefreshTimer <= 0f)
        {
            pathfinder.FindPath(owner.Position, targetPos, currentPath);
            pathIndex = 0;
            pathRefreshTimer = PathRefreshInterval;
        }

        // No path found — fall back to direct
        if (currentPath.Count == 0)
            return ((Vector2)(targetPos - owner.Position)).normalized;

        // Advance past reached waypoints
        while (pathIndex < currentPath.Count)
        {
            float distSq = ((Vector2)(currentPath[pathIndex] - owner.Position)).sqrMagnitude;
            if (distSq > WaypointReachDist * WaypointReachDist)
                break;
            pathIndex++;
        }

        // All waypoints reached
        if (pathIndex >= currentPath.Count)
        {
            currentPath.Clear();
            return ((Vector2)(targetPos - owner.Position)).normalized;
        }

        // Move toward current waypoint
        return ((Vector2)(currentPath[pathIndex] - owner.Position)).normalized;
    }

    // ── Wall Avoidance ──

    /// <summary>
    /// Steer direction away from map boundaries when close to edges.
    /// Uses a soft margin to gradually push AI back toward center.
    /// </summary>
    private Vector2 SteerAwayFromBounds(Vector2 position, Vector2 direction)
    {
        MapManager map = MapManager.Instance;
        if (map == null) return direction;

        Vector2 mapMin = map.MapMin;
        Vector2 mapMax = map.MapMax;
        float margin = 3f; // start steering when within 3 units of edge

        Vector2 steer = Vector2.zero;

        if (position.x < mapMin.x + margin)
            steer.x += 1f - (position.x - mapMin.x) / margin;
        else if (position.x > mapMax.x - margin)
            steer.x -= 1f - (mapMax.x - position.x) / margin;

        if (position.y < mapMin.y + margin)
            steer.y += 1f - (position.y - mapMin.y) / margin;
        else if (position.y > mapMax.y - margin)
            steer.y -= 1f - (mapMax.y - position.y) / margin;

        if (steer.sqrMagnitude > 0.001f)
        {
            // Blend: the closer to edge, the stronger the steer
            direction = (direction + steer * 2f).normalized;
        }

        return direction;
    }

    private Vector2 AdjustForWalls(Vector2 position, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, direction, wallAvoidDistance, wallMask);

        if (hit.collider != null)
        {
            // In Wander state, reset timer to pick a new direction next frame
            if (currentState == AIState.Wander)
                wanderTimer = 0f;

            // Try rotated directions: ±45°, ±90°
            float[] angles = { 45f, -45f, 90f, -90f };
            for (int i = 0; i < angles.Length; i++)
            {
                Vector2 rotated = RotateVector(direction, angles[i]);
                RaycastHit2D check = Physics2D.Raycast(position, rotated, wallAvoidDistance, wallMask);
                if (check.collider == null)
                    return rotated;
            }

            // All directions blocked — reverse
            return -direction;
        }

        return direction;
    }

    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ── Debug Gizmos ──

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying) return;
        if (owner == null) return;

        Vector3 pos = owner.Position;

        // Vòng tròn tầm nhìn enemy (đỏ) và item (xanh lá)
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.3f, 0.25f);
        UnityEditor.Handles.DrawWireDisc(pos, Vector3.forward, enemyPerceptionRadius);
        UnityEditor.Handles.color = new Color(0.3f, 1f, 0.3f, 0.25f);
        UnityEditor.Handles.DrawWireDisc(pos, Vector3.forward, itemPerceptionRadius);

        // Đường đi A* (từ vị trí hiện tại → các waypoint)
        if (currentPath.Count > 0 && pathIndex < currentPath.Count)
        {
            Color pathColor = GetStateColor();
            Gizmos.color = pathColor;

            // Đoạn từ AI đến waypoint hiện tại
            Gizmos.DrawLine(pos, currentPath[pathIndex]);

            // Các đoạn waypoint còn lại
            for (int i = pathIndex; i < currentPath.Count - 1; i++)
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);

            // Vẽ điểm tại mỗi waypoint
            for (int i = pathIndex; i < currentPath.Count; i++)
            {
                Gizmos.DrawWireSphere(currentPath[i], 0.2f);
            }
        }

        // Đường thẳng đến target (nét đứt bằng cách vẽ mờ hơn)
        Vector3 targetPos = GetTargetPosition();
        if (targetPos != Vector3.zero)
        {
            Gizmos.color = GetStateColor() * new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawLine(pos, targetPos);

            // Diamond marker tại target
            Gizmos.color = GetStateColor();
            Gizmos.DrawWireSphere(targetPos, 0.35f);
        }

        // Mũi tên hướng di chuyển hiện tại
        if (currentDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Vector3 arrowEnd = pos + (Vector3)(currentDirection * 1.5f);
            Gizmos.DrawLine(pos, arrowEnd);
        }
    }

    private Color GetStateColor()
    {
        return currentState switch
        {
            AIState.Wander  => Color.white,
            AIState.Collect => Color.green,
            AIState.Chase   => Color.red,
            AIState.Flee    => new Color(1f, 0.5f, 0f),
            _               => Color.cyan
        };
    }

    private Vector3 GetTargetPosition()
    {
        return currentState switch
        {
            AIState.Chase   => chaseTarget != null ? chaseTarget.Position : Vector3.zero,
            AIState.Collect => collectTarget != null && collectTarget.IsActive ? collectTarget.Position : Vector3.zero,
            AIState.Flee    => fleeTarget != null ? fleeTarget.Position : Vector3.zero,
            _               => Vector3.zero
        };
    }
#endif
}
