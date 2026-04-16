using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class CharacterStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterBase owner;
    [SerializeField] private SwordOrbit orbit;

    private MapManager map;
    private CharacterManager charMgr;
    private ItemManager itemMgr;

    [Header("AI Settings")]
    [SerializeField] private float visionRadius = 15f;
    [SerializeField] private float separationRadius = 1.2f;

    [SerializeField] private float fleeSpeedMultiplier = 1.6f;
    [SerializeField] private float fleeSpeedDuration = 1.2f;
    [SerializeField] private float fleeSpeedCooldown = 5f;

    [SerializeField] private float stateMinDuration = 0.4f;

    private float visionRadiusSq;
    private float separationRadiusSq;

    public CharacterBase Owner => owner;
    public SwordOrbit Orbit => orbit;
    public MapManager Map => map;
    public GridPathfinder Pathfinder => map != null ? map.Pathfinder : null;
    public CharacterManager CharMgr => charMgr;
    public ItemManager ItemMgr => itemMgr;

    public float VisionRadius => visionRadius;
    public float VisionRadiusSq => visionRadiusSq;
    public float FleeSpeedMultiplier => fleeSpeedMultiplier;
    public float FleeSpeedDuration => fleeSpeedDuration;
    public float FleeSpeedCooldown => fleeSpeedCooldown;
    public float StateMinDuration => stateMinDuration;

    public float SeparationRadius => separationRadius;
    private const float SeparationForce = 8f;

    public ICharacterState CurrentState { get; private set; }
    public float StateTimer { get; set; }
    public float FleeBoostTimer { get; set; }
    public float FleeCooldownTimer { get; set; }

    public Vector3 CachedPosition { get; private set; }

    public readonly List<Vector3> PathBuffer = new(32);
    public readonly List<CharacterBase> NearbyCharacters = new(16);
    public readonly List<Sword> NearbySwords = new(16);

    public readonly WanderState Wander = new();
    public readonly CollectSwordState CollectSword = new();
    public readonly AttackState Attack = new();
    public readonly FleeState Flee = new();
    public readonly DeadState Dead = new();
    public readonly KnockbackState Knockback = new();

    private void Awake()
    {
        if (owner == null) owner = GetComponent<CharacterBase>();
        if (orbit == null && owner != null) orbit = owner.GetSwordOrbit();
        
        if (map == null) map = MapManager.Instance;
        if (charMgr == null) charMgr = CharacterManager.Instance;
        if (itemMgr == null) itemMgr = ItemManager.Instance;

        // Cache squared values
        visionRadiusSq = visionRadius * visionRadius;
        separationRadiusSq = separationRadius * separationRadius;

        if (map == null || charMgr == null || itemMgr == null)
            Debug.LogWarning($"[{gameObject.name}] CharacterStateMachine: Some managers are null! Please assign in Inspector or ensure singletons are initialized.");
    }

    private void Start()
    {
        ChangeState(Wander);
    }

    public void ManagedUpdate(float deltaTime)
    {
        CachedPosition = owner.transform.position;

        StateTimer += deltaTime;
        if (FleeCooldownTimer > 0f) FleeCooldownTimer -= deltaTime;
        if (FleeBoostTimer > 0f) FleeBoostTimer -= deltaTime;

        if (map != null && map.IsWall(CachedPosition))
        {
            TryEscapeWall();
            CachedPosition = owner.transform.position;
        }

        CurrentState?.Execute(this, deltaTime);
        ApplySeparation(deltaTime);
    }

    public void ChangeState(ICharacterState newState)
    {
        if (newState == CurrentState) return;
        CurrentState?.Exit(this);
        CurrentState = newState;
        StateTimer = 0f;
        CurrentState.Enter(this);
    }

    public void OnTakeDamage(CharacterBase attacker)
    {
        if (CurrentState == Dead) return;
        if (owner.CurrentHp <= 0f) { ChangeState(Dead); return; }
        if (CurrentState == Knockback) return;

        if (attacker != null)
        {
            if (MySwordCount > 5)
            {
                Attack.SetTarget(attacker);
                ChangeState(Attack);
            }
            else
            {
                Flee.SetThreat(attacker);
                ChangeState(Flee);
            }
        }
    }

    public void OnKnockback()
    {
        if (CurrentState == Dead) return;
        if (CurrentState == Flee) return;
        ChangeState(Knockback);
    }

    public int MySwordCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => orbit != null ? orbit.SwordCount : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetCurrentSpeed()
    {
        float speed = owner.MoveSpeed;
        if (FleeBoostTimer > 0f) speed *= fleeSpeedMultiplier;
        return speed;
    }

    public bool TryActivateFleeBoost()
    {
        if (FleeCooldownTimer > 0f) return false;
        FleeBoostTimer = fleeSpeedDuration;
        FleeCooldownTimer = fleeSpeedCooldown;
        return true;
    }

    public bool MoveToward(Vector3 target, float speed, float deltaTime, float arriveThreshold = 0.3f)
    {
        float posX = CachedPosition.x, posY = CachedPosition.y, posZ = CachedPosition.z;
        float dx = target.x - posX;
        float dy = target.y - posY;
        float distSq = dx * dx + dy * dy;
        float threshSq = arriveThreshold * arriveThreshold;

        if (distSq <= threshSq) return true;

        float dist = Mathf.Sqrt(distSq);
        float step = speed * deltaTime;

        float nextX, nextY;
        if (step >= dist)
        {
            nextX = target.x; nextY = target.y;
        }
        else
        {
            float inv = step / dist;
            nextX = posX + dx * inv;
            nextY = posY + dy * inv;
        }

        ValidateMove(posX, posY, ref nextX, ref nextY, posZ);

        float movedDx = nextX - posX;
        float movedDy = nextY - posY;
        if (movedDx * movedDx + movedDy * movedDy < 0.001f)
            return true;

        Vector3 newPos = new Vector3(nextX, nextY, posZ);
        owner.transform.position = newPos;
        CachedPosition = newPos;

        float remainDx = target.x - nextX;
        float remainDy = target.y - nextY;
        return (remainDx * remainDx + remainDy * remainDy) <= threshSq;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ValidateMove(float fromX, float fromY, ref float toX, ref float toY, float z)
    {
        if (map == null) return;

        if (!map.IsBlockedWorld(new Vector3(toX, toY, z)))
        {
            float midX = (fromX + toX) * 0.5f;
            float midY = (fromY + toY) * 0.5f;
            if (!map.IsBlockedWorld(new Vector3(midX, midY, z)))
                return;
        }

        if (!map.IsBlockedWorld(new Vector3(toX, fromY, z)))
        {
            float midX = (fromX + toX) * 0.5f;
            if (!map.IsBlockedWorld(new Vector3(midX, fromY, z)))
            {
                toY = fromY;
                return;
            }
        }

        if (!map.IsBlockedWorld(new Vector3(fromX, toY, z)))
        {
            float midY = (fromY + toY) * 0.5f;
            if (!map.IsBlockedWorld(new Vector3(fromX, midY, z)))
            {
                toX = fromX;
                return;
            }
        }

        toX = fromX;
        toY = fromY;
    }

    public Vector3 ValidateMove(Vector3 from, Vector3 to)
    {
        float toX = to.x, toY = to.y;
        ValidateMove(from.x, from.y, ref toX, ref toY, from.z);
        return new Vector3(toX, toY, from.z);
    }

    public bool MoveAlongPath(ref int pathIndex, float speed, float deltaTime)
    {
        if (pathIndex >= PathBuffer.Count) return true;
        if (MoveToward(PathBuffer[pathIndex], speed, deltaTime))
        {
            pathIndex++;
            return pathIndex >= PathBuffer.Count;
        }
        return false;
    }

    public CharacterBase FindWeakerTarget()
    {
        if (charMgr == null) return null;
        int mySwords = MySwordCount;
        if (mySwords <= 0) return null;

        charMgr.GetNearbyCharacters(CachedPosition, visionRadius, NearbyCharacters);

        CharacterBase best = null;
        float bestDistSq = float.MaxValue;
        float myX = CachedPosition.x, myY = CachedPosition.y;

        int count = NearbyCharacters.Count;
        for (int i = 0; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == owner || other.CurrentHp <= 0f) continue;

            SwordOrbit otherOrbit = other.GetSwordOrbit();
            int otherSwords = otherOrbit != null ? otherOrbit.SwordCount : 0;
            if (otherSwords >= mySwords) continue;

            float dx = other.Position.x - myX;
            float dy = other.Position.y - myY;
            float distSq = dx * dx + dy * dy;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = other;
            }
        }
        return best;
    }

    public Sword FindBestSword()
    {
        if (itemMgr == null) return null;

        itemMgr.GetNearbySwords(CachedPosition, visionRadius, NearbySwords);
        int count = NearbySwords.Count;
        if (count == 0) return null;
        if (count == 1) return NearbySwords[0];

        float myX = CachedPosition.x, myY = CachedPosition.y;

        // Find nearest by Euclidean distance (cheap)
        Sword best = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Sword s = NearbySwords[i];
            float dx = s.Position.x - myX;
            float dy = s.Position.y - myY;
            float distSq = dx * dx + dy * dy;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = s;
            }
        }

        // Note: Pathfinding validation removed for performance
        // States will handle unreachable swords via stuck detection
        return best;
    }

    private void ApplySeparation(float deltaTime)
    {
        if (charMgr == null || CurrentState == Dead) return;

        float myX = CachedPosition.x, myY = CachedPosition.y;
        charMgr.GetNearbyCharacters(CachedPosition, separationRadius, NearbyCharacters);

        float pushX = 0f, pushY = 0f;
        int count = NearbyCharacters.Count;

        for (int i = 0; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == owner || other.CurrentHp <= 0f) continue;

            float dx = myX - other.Position.x;
            float dy = myY - other.Position.y;
            float distSq = dx * dx + dy * dy;

            if (distSq < 0.001f)
            {
                // Deterministic random push when exactly overlapping
                float angle = (owner.GetInstanceID() * 2654435761u & 0xFFFF) * (Mathf.PI * 2f / 65536f);
                pushX += Mathf.Cos(angle);
                pushY += Mathf.Sin(angle);
                continue;
            }

            if (distSq >= separationRadiusSq) continue;

            // Optimize: avoid Sqrt by using distSq directly
            float dist = Mathf.Sqrt(distSq);
            float strength = (separationRadius - dist) / separationRadius;
            float factor = strength / dist; // Combined invDist * strength
            pushX += dx * factor;
            pushY += dy * factor;
        }

        if (pushX == 0f && pushY == 0f) return;

        float magSq = pushX * pushX + pushY * pushY;
        if (magSq > 0f)
        {
            float invMag = 1f / Mathf.Sqrt(magSq);
            float step = SeparationForce * deltaTime;
            float newX = myX + pushX * invMag * step;
            float newY = myY + pushY * invMag * step;
            ValidateMove(myX, myY, ref newX, ref newY, CachedPosition.z);
            Vector3 newPos = new Vector3(newX, newY, CachedPosition.z);
            owner.transform.position = newPos;
            CachedPosition = newPos;
        }
    }

    private void TryEscapeWall()
    {
        Vector3 pos = CachedPosition;
        Vector2Int cell = map.WorldToCell(pos);

        float bestDistSq = float.MaxValue;
        int bestCol = cell.x, bestRow = cell.y;
        bool found = false;

        for (int r = 1; r <= 5; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx > -r && dx < r && dy > -r && dy < r) continue;

                    int nc = cell.x + dx;
                    int nr = cell.y + dy;
                    if (map.IsBlocked(nc, nr)) continue;

                    Vector3 candidate = map.CellToWorld(nc, nr);
                    float ddx = candidate.x - pos.x;
                    float ddy = candidate.y - pos.y;
                    float distSq = ddx * ddx + ddy * ddy;

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestCol = nc;
                        bestRow = nr;
                        found = true;
                    }
                }
            }
            if (found) break;
        }

        if (found)
        {
            Vector3 target = map.CellToWorld(bestCol, bestRow);
            owner.transform.position = new Vector3(target.x, target.y, pos.z);
        }
    }
}
