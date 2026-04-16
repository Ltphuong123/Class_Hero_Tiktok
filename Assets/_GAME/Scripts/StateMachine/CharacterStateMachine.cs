using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Bộ điều khiển state machine cho CharacterBase.
/// Được gọi từ CharacterBase.ManagedUpdate mỗi frame.
/// </summary>
public class CharacterStateMachine
{
    // --- Cached references ---
    public CharacterBase Owner { get; private set; }
    public SwordOrbit Orbit { get; private set; }
    public MapManager Map { get; private set; }
    public GridPathfinder Pathfinder { get; private set; }
    public CharacterManager CharMgr { get; private set; }
    public ItemManager ItemMgr { get; private set; }

    // --- Config ---
    public float VisionRadius { get; private set; }
    public float VisionRadiusSq { get; private set; }
    public float FleeSpeedMultiplier { get; private set; }
    public float FleeSpeedDuration { get; private set; }
    public float FleeSpeedCooldown { get; private set; }
    public float StateMinDuration { get; private set; }

    // --- Separation ---
    public float SeparationRadius { get; private set; }
    private float SeparationRadiusSq => SeparationRadius * SeparationRadius;
    private const float SeparationForce = 8f;

    // --- State ---
    public ICharacterState CurrentState { get; private set; }
    public float StateTimer { get; set; }
    public float FleeBoostTimer { get; set; }
    public float FleeCooldownTimer { get; set; }

    // --- Cached position (cập nhật 1 lần đầu mỗi frame) ---
    public Vector3 CachedPosition { get; private set; }

    // --- Reusable buffers ---
    public readonly List<Vector3> PathBuffer = new(32);
    public readonly List<CharacterBase> NearbyCharacters = new(16);
    public readonly List<Sword> NearbySwords = new(16);

    // --- Shared state instances ---
    public readonly WanderState Wander = new();
    public readonly CollectSwordState CollectSword = new();
    public readonly AttackState Attack = new();
    public readonly FleeState Flee = new();
    public readonly DeadState Dead = new();
    public readonly KnockbackState Knockback = new();

    public CharacterStateMachine(
        CharacterBase owner,
        float visionRadius = 15f,
        float separationRadius = 1.2f,
        float fleeSpeedMultiplier = 1.6f,
        float fleeSpeedDuration = 1.2f,
        float fleeSpeedCooldown = 5f,
        float stateMinDuration = 0.4f)
    {
        Owner = owner;
        Orbit = owner.GetSwordOrbit();
        VisionRadius = visionRadius;
        VisionRadiusSq = visionRadius * visionRadius;
        SeparationRadius = separationRadius;
        FleeSpeedMultiplier = fleeSpeedMultiplier;
        FleeSpeedDuration = fleeSpeedDuration;
        FleeSpeedCooldown = fleeSpeedCooldown;
        StateMinDuration = stateMinDuration;

        Map = MapManager.Instance;
        Pathfinder = Map != null ? Map.Pathfinder : null;
        CharMgr = CharacterManager.Instance;
        ItemMgr = ItemManager.Instance;
    }

    public void Start() => ChangeState(Wander);

    public void Update(float deltaTime)
    {
        // Lazy re-cache managers nếu chưa sẵn sàng lúc khởi tạo
        if (Map == null || CharMgr == null || ItemMgr == null)
        {
            TryRecacheManagers();
        }

        CachedPosition = Owner.transform.position;

        StateTimer += deltaTime;
        if (FleeCooldownTimer > 0f) FleeCooldownTimer -= deltaTime;
        if (FleeBoostTimer > 0f) FleeBoostTimer -= deltaTime;

        // Thoát tường
        if (Map != null && Map.IsWall(CachedPosition))
        {
            TryEscapeWall();
            CachedPosition = Owner.transform.position;
        }

        CurrentState?.Execute(this, deltaTime);

        // Separation
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
        if (Owner.CurrentHp <= 0f) { ChangeState(Dead); return; }
        if (CurrentState == Knockback) return;

        if (attacker != null)
        {
            if (MySwordCount > 5)
            {
                // Nhiều kiếm → phản đòn
                Attack.SetTarget(attacker);
                ChangeState(Attack);
            }
            else
            {
                // 5 kiếm hoặc ít hơn → luôn chạy trốn
                Flee.SetThreat(attacker);
                ChangeState(Flee);
            }
        }
    }

    public void OnKnockback()
    {
        if (CurrentState == Dead) return;
        if (CurrentState == Flee) return; // Đang chạy trốn → bỏ qua knockback
        ChangeState(Knockback);
    }

    private void TryRecacheManagers()
    {
        if (Map == null)
        {
            Map = MapManager.Instance;
            if (Map != null) Pathfinder = Map.Pathfinder;
        }
        if (CharMgr == null) CharMgr = CharacterManager.Instance;
        if (ItemMgr == null) ItemMgr = ItemManager.Instance;
    }

    // --- Properties ---
    public int MySwordCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Orbit != null ? Orbit.SwordCount : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetCurrentSpeed()
    {
        float speed = Owner.MoveSpeed;
        if (FleeBoostTimer > 0f) speed *= FleeSpeedMultiplier;
        return speed;
    }

    public bool TryActivateFleeBoost()
    {
        if (FleeCooldownTimer > 0f) return false;
        FleeBoostTimer = FleeSpeedDuration;
        FleeCooldownTimer = FleeSpeedCooldown;
        return true;
    }

    // --- Movement ---

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

        // Validate
        ValidateMove(posX, posY, ref nextX, ref nextY, posZ);

        float movedDx = nextX - posX;
        float movedDy = nextY - posY;
        if (movedDx * movedDx + movedDy * movedDy < 0.001f)
            return true;

        Vector3 newPos = new Vector3(nextX, nextY, posZ);
        Owner.transform.position = newPos;
        CachedPosition = newPos;

        float remainDx = target.x - nextX;
        float remainDy = target.y - nextY;
        return (remainDx * remainDx + remainDy * remainDy) <= threshSq;
    }

    /// <summary>
    /// Validate di chuyển — sửa nextX/nextY in-place, tránh tạo Vector3 tạm.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ValidateMove(float fromX, float fromY, ref float toX, ref float toY, float z)
    {
        if (Map == null) return;

        // Check điểm đến + midpoint
        if (!Map.IsBlockedWorld(new Vector3(toX, toY, z)))
        {
            float midX = (fromX + toX) * 0.5f;
            float midY = (fromY + toY) * 0.5f;
            if (!Map.IsBlockedWorld(new Vector3(midX, midY, z)))
                return;
        }

        // Slide X
        if (!Map.IsBlockedWorld(new Vector3(toX, fromY, z)))
        {
            float midX = (fromX + toX) * 0.5f;
            if (!Map.IsBlockedWorld(new Vector3(midX, fromY, z)))
            {
                toY = fromY;
                return;
            }
        }

        // Slide Y
        if (!Map.IsBlockedWorld(new Vector3(fromX, toY, z)))
        {
            float midY = (fromY + toY) * 0.5f;
            if (!Map.IsBlockedWorld(new Vector3(fromX, midY, z)))
            {
                toX = fromX;
                return;
            }
        }

        toX = fromX;
        toY = fromY;
    }

    /// <summary>
    /// Overload Vector3 cho compatibility với AttackState lùi.
    /// </summary>
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

    // --- Scanning ---

    public CharacterBase FindWeakerTarget()
    {
        if (CharMgr == null) return null;
        int mySwords = MySwordCount;
        if (mySwords <= 0) return null;

        CharMgr.GetNearbyCharacters(CachedPosition, VisionRadius, NearbyCharacters);

        CharacterBase best = null;
        float bestDistSq = float.MaxValue;
        float myX = CachedPosition.x, myY = CachedPosition.y;

        int count = NearbyCharacters.Count;
        for (int i = 0; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == Owner || other.CurrentHp <= 0f) continue;

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

    /// <summary>
    /// Tìm kiếm rơi gần nhất — dùng Euclidean distance thay vì A* path distance.
    /// Chỉ dùng A* khi có nhiều kiếm cùng khoảng cách (top 3 gần nhất).
    /// </summary>
    public Sword FindBestSword()
    {
        if (ItemMgr == null) return null;

        ItemMgr.GetNearbySwords(CachedPosition, VisionRadius, NearbySwords);
        int count = NearbySwords.Count;
        if (count == 0) return null;

        float myX = CachedPosition.x, myY = CachedPosition.y;

        if (count == 1) return NearbySwords[0];

        // Tìm top gần nhất bằng Euclidean (O(n), không cần A*)
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

        // Nếu có pathfinder, verify kiếm gần nhất có đường đi hợp lệ
        if (Pathfinder != null && best != null)
        {
            float pathDist = Pathfinder.FindPathDistance(CachedPosition, best.Position);
            if (pathDist >= float.MaxValue)
            {
                // Kiếm gần nhất không có đường → tìm kiếm khác có đường
                best = null;
                bestDistSq = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    Sword s = NearbySwords[i];
                    float dist = Pathfinder.FindPathDistance(CachedPosition, s.Position);
                    if (dist < bestDistSq)
                    {
                        bestDistSq = dist;
                        best = s;
                    }
                }
            }
        }

        return best;
    }

    // --- Separation & Wall escape ---

    private void ApplySeparation(float deltaTime)
    {
        if (CharMgr == null || CurrentState == Dead) return;

        float myX = CachedPosition.x, myY = CachedPosition.y;
        CharMgr.GetNearbyCharacters(CachedPosition, SeparationRadius, NearbyCharacters);

        float pushX = 0f, pushY = 0f;
        int count = NearbyCharacters.Count;

        for (int i = 0; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == Owner || other.CurrentHp <= 0f) continue;

            float dx = myX - other.Position.x;
            float dy = myY - other.Position.y;
            float distSq = dx * dx + dy * dy;

            if (distSq < 0.001f)
            {
                // Trùng vị trí — dùng hash thay vì Random để deterministic
                float angle = (Owner.GetInstanceID() * 2654435761u & 0xFFFF) * (Mathf.PI * 2f / 65536f);
                pushX += Mathf.Cos(angle);
                pushY += Mathf.Sin(angle);
                continue;
            }

            if (distSq >= SeparationRadiusSq) continue;

            float dist = Mathf.Sqrt(distSq);
            float strength = (SeparationRadius - dist) / SeparationRadius;
            float invDist = 1f / dist;
            pushX += dx * invDist * strength;
            pushY += dy * invDist * strength;
        }

        if (pushX == 0f && pushY == 0f) return;

        float mag = Mathf.Sqrt(pushX * pushX + pushY * pushY);
        if (mag > 0f)
        {
            float step = SeparationForce * deltaTime / mag;
            float newX = myX + pushX * step;
            float newY = myY + pushY * step;
            ValidateMove(myX, myY, ref newX, ref newY, CachedPosition.z);
            Vector3 newPos = new Vector3(newX, newY, CachedPosition.z);
            Owner.transform.position = newPos;
            CachedPosition = newPos;
        }
    }

    private void TryEscapeWall()
    {
        Vector3 pos = CachedPosition;
        Vector2Int cell = Map.WorldToCell(pos);

        float bestDistSq = float.MaxValue;
        int bestCol = cell.x, bestRow = cell.y;
        bool found = false;

        // Scan vòng ngoài trước — early exit khi tìm được ô mở
        for (int r = 1; r <= 5; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    // Chỉ check vòng ngoài của radius r
                    if (dx > -r && dx < r && dy > -r && dy < r) continue;

                    int nc = cell.x + dx;
                    int nr = cell.y + dy;
                    if (Map.IsBlocked(nc, nr)) continue;

                    Vector3 candidate = Map.CellToWorld(nc, nr);
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
            if (found) break; // Tìm được ở vòng r → không cần check vòng xa hơn
        }

        if (found)
        {
            Vector3 target = Map.CellToWorld(bestCol, bestRow);
            Owner.transform.position = new Vector3(target.x, target.y, pos.z);
        }
    }
}
