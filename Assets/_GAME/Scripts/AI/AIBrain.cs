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
    [SerializeField] private float perceptionRadius = 10f;
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
    private bool hasLoggedManagerWarning;

    // ── Movement smoothing ──
    private Vector2 currentDirection;
    private Vector2 targetDirection;
    private const float DirectionSmoothSpeed = 8f;

    // ── State change hysteresis ──
    private float stateChangeTimer;
    private const float MinStateHoldTime = 0.5f; // don't switch state too fast

    // ── Cached references ──
    private CharacterBase owner;

    // ── Read-only properties ──
    public AIState CurrentState => currentState;
    public CharacterBase Owner => owner;

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
        charMgr.GetNearbyCharacters(owner.Position, perceptionRadius, nearbyEnemies);

        // Exclude self from results
        for (int i = nearbyEnemies.Count - 1; i >= 0; i--)
        {
            if (nearbyEnemies[i] == owner)
                nearbyEnemies.RemoveAt(i);
        }

        // Query nearest items
        Sword nearestSword = itemMgr.GetNearestSword(owner.Position, perceptionRadius);
        ICollectibleItem nearestItem = itemMgr.GetNearestItem(owner.Position, perceptionRadius);

        // Compute scores
        int ownSwords = owner.GetSwordOrbit() != null ? owner.GetSwordOrbit().SwordCount : 0;
        float ownHp = owner.CurrentHp;
        float maxHp = owner.MaxHp;

        scores[0] = AIScoring.ComputeWanderScore(nearbyEnemies.Count, nearestSword != null || nearestItem != null);
        scores[1] = AIScoring.ComputeCollectScore(nearestSword != null, nearestItem != null, ownSwords, 8);

        // Best chase score across all enemies
        float bestChaseScore = 0f;
        CharacterBase bestChaseTarget = null;
        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            CharacterBase enemy = nearbyEnemies[i];
            int enemySwords = enemy.GetSwordOrbit() != null ? enemy.GetSwordOrbit().SwordCount : 0;
            float score = AIScoring.ComputeChaseScore(ownSwords, ownHp, enemySwords, enemy.CurrentHp, maxHp);
            if (score > bestChaseScore)
            {
                bestChaseScore = score;
                bestChaseTarget = enemy;
            }
        }
        scores[2] = bestChaseScore;

        // Best flee score across all enemies
        float bestFleeScore = 0f;
        CharacterBase bestFleeTarget = null;
        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            CharacterBase enemy = nearbyEnemies[i];
            int enemySwords = enemy.GetSwordOrbit() != null ? enemy.GetSwordOrbit().SwordCount : 0;
            float score = AIScoring.ComputeFleeScore(ownSwords, ownHp, enemySwords, enemy.CurrentHp, maxHp);
            if (score > bestFleeScore)
            {
                bestFleeScore = score;
                bestFleeTarget = enemy;
            }
        }
        scores[3] = bestFleeScore;

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
                collectTarget = nearestSword != null ? nearestSword : nearestItem;
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

    // ── ExecuteMovement — called by AIManager every frame ──

    public void ExecuteMovement(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (owner == null) return;

        float moveSpeed = owner.MoveSpeed;
        targetDirection = Vector2.zero;

        switch (currentState)
        {
            case AIState.Wander:
                wanderTimer -= deltaTime;
                if (wanderTimer <= 0f)
                {
                    // Smooth wander: rotate slightly from current direction instead of fully random
                    float angle = Random.Range(-90f, 90f);
                    wanderDirection = RotateVector(wanderDirection, angle).normalized;
                    if (wanderDirection == Vector2.zero)
                        wanderDirection = Random.insideUnitCircle.normalized;
                    wanderTimer = wanderChangeInterval + Random.Range(-0.5f, 0.5f);
                }
                targetDirection = wanderDirection;
                break;

            case AIState.Collect:
                if (collectTarget == null || !collectTarget.IsActive)
                {
                    Evaluate();
                    return;
                }
                targetDirection = ((Vector2)(collectTarget.Position - owner.Position)).normalized;
                break;

            case AIState.Chase:
                if (chaseTarget == null)
                {
                    Evaluate();
                    return;
                }
                targetDirection = ((Vector2)(chaseTarget.Position - owner.Position)).normalized;
                break;

            case AIState.Flee:
                if (fleeTarget == null)
                {
                    Evaluate();
                    return;
                }
                targetDirection = ((Vector2)(owner.Position - fleeTarget.Position)).normalized;
                break;
        }

        // Smooth direction transition (prevents jerky movement)
        currentDirection = Vector2.Lerp(currentDirection, targetDirection, DirectionSmoothSpeed * deltaTime);
        if (currentDirection.sqrMagnitude > 0.001f)
            currentDirection = currentDirection.normalized;

        // Steer away from map edges (soft boundary)
        currentDirection = SteerAwayFromBounds((Vector2)owner.Position, currentDirection);

        // Apply wall avoidance
        Vector2 finalDir = AdjustForWalls((Vector2)owner.Position, currentDirection);

        // Apply movement
        owner.transform.position += (Vector3)(finalDir * moveSpeed * deltaTime);
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
}
