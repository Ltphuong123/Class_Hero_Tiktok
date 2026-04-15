using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bộ điều khiển state machine cho CharacterBase.
/// Được gọi từ CharacterBase.ManagedUpdate mỗi frame.
/// </summary>
public class CharacterStateMachine
{
    // --- Cached references (set 1 lần khi khởi tạo) ---
    public CharacterBase Owner { get; private set; }
    public SwordOrbit Orbit { get; private set; }
    public MapManager Map { get; private set; }
    public GridPathfinder Pathfinder { get; private set; }
    public CharacterManager CharMgr { get; private set; }
    public ItemManager ItemMgr { get; private set; }

    // --- Config ---
    public float VisionRadius { get; private set; }
    public float AttackKeepDistance { get; private set; }
    public float FleeSpeedMultiplier { get; private set; }
    public float FleeSpeedDuration { get; private set; }
    public float FleeSpeedCooldown { get; private set; }
    public float StateMinDuration { get; private set; }

    // --- Shared state data ---
    public ICharacterState CurrentState { get; private set; }
    public float StateTimer { get; set; }

    // Flee speed boost tracking
    public float FleeBoostTimer { get; set; }
    public float FleeCooldownTimer { get; set; }

    // Reusable buffers (tránh GC alloc mỗi frame)
    public readonly List<Vector3> PathBuffer = new(32);
    public readonly List<CharacterBase> NearbyCharacters = new(16);
    public readonly List<Sword> NearbySwords = new(16);

    // Shared state instances (1 instance mỗi loại, tái sử dụng)
    public readonly WanderState Wander = new();
    public readonly CollectSwordState CollectSword = new();
    public readonly AttackState Attack = new();
    public readonly FleeState Flee = new();
    public readonly DeadState Dead = new();
    public readonly KnockbackState Knockback = new();

    public CharacterStateMachine(
        CharacterBase owner,
        float visionRadius = 15f,
        float attackKeepDistance = 1f,
        float fleeSpeedMultiplier = 1.6f,
        float fleeSpeedDuration = 1.2f,
        float fleeSpeedCooldown = 5f,
        float stateMinDuration = 0.4f)
    {
        Owner = owner;
        Orbit = owner.GetSwordOrbit();
        VisionRadius = visionRadius;
        AttackKeepDistance = attackKeepDistance;
        FleeSpeedMultiplier = fleeSpeedMultiplier;
        FleeSpeedDuration = fleeSpeedDuration;
        FleeSpeedCooldown = fleeSpeedCooldown;
        StateMinDuration = stateMinDuration;

        // Cache managers
        Map = MapManager.Instance;
        Pathfinder = Map != null ? Map.Pathfinder : null;
        CharMgr = CharacterManager.Instance;
        ItemMgr = ItemManager.Instance;
    }

    public void Start()
    {
        ChangeState(Wander);
    }

    public void Update(float deltaTime)
    {
        StateTimer += deltaTime;

        // Cooldown flee speed boost
        if (FleeCooldownTimer > 0f) FleeCooldownTimer -= deltaTime;
        if (FleeBoostTimer > 0f) FleeBoostTimer -= deltaTime;

        CurrentState?.Execute(this, deltaTime);
    }

    public void ChangeState(ICharacterState newState)
    {
        if (newState == CurrentState) return;

        CurrentState?.Exit(this);
        CurrentState = newState;
        StateTimer = 0f;
        CurrentState.Enter(this);
    }

    /// <summary>
    /// Gọi khi bị đánh. Xử lý reactive transition (ưu tiên cao nhất sau Dead).
    /// </summary>
    public void OnTakeDamage(CharacterBase attacker)
    {
        if (CurrentState == Dead) return;

        if (Owner.CurrentHp <= 0f)
        {
            ChangeState(Dead);
            return;
        }

        // Đang knockback thì không đổi state (knockback tự xử lý)
        if (CurrentState == Knockback) return;

        if (attacker != null)
        {
            int mySwords = Orbit != null ? Orbit.SwordCount : 0;

            if (mySwords > 0)
            {
                // Có kiếm → phản đòn
                Attack.SetTarget(attacker);
                ChangeState(Attack);
            }
            else
            {
                // Không kiếm → chạy trốn
                Flee.SetThreat(attacker);
                ChangeState(Flee);
            }
        }
    }

    /// <summary>
    /// Gọi khi bị knockback. Chuyển sang KnockbackState.
    /// </summary>
    public void OnKnockback()
    {
        if (CurrentState == Dead) return;
        ChangeState(Knockback);
    }

    // --- Helper methods dùng chung cho các state ---

    public int MySwordCount => Orbit != null ? Orbit.SwordCount : 0;

    /// <summary>
    /// Di chuyển owner về phía target position với tốc độ cho trước.
    /// Trả về true nếu đã đến nơi (khoảng cách < threshold).
    /// </summary>
    public bool MoveToward(Vector3 target, float speed, float deltaTime, float arriveThreshold = 0.3f)
    {
        Vector3 pos = Owner.Position;
        float dx = target.x - pos.x;
        float dy = target.y - pos.y;
        float distSq = dx * dx + dy * dy;
        float threshSq = arriveThreshold * arriveThreshold;

        if (distSq <= threshSq) return true;

        float dist = Mathf.Sqrt(distSq);
        float step = speed * deltaTime;
        if (step >= dist)
        {
            Owner.transform.position = new Vector3(target.x, target.y, pos.z);
            return true;
        }

        float inv = step / dist;
        Owner.transform.position = new Vector3(pos.x + dx * inv, pos.y + dy * inv, pos.z);
        return false;
    }

    /// <summary>
    /// Di chuyển theo path buffer. Trả về true khi đến cuối path.
    /// pathIndex được truyền bằng ref để state tự track.
    /// </summary>
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

    /// <summary>
    /// Tìm CharacterBase yếu hơn (ít kiếm hơn) gần nhất trong tầm nhìn.
    /// </summary>
    public CharacterBase FindWeakerTarget()
    {
        if (CharMgr == null) return null;

        int mySwords = MySwordCount;
        if (mySwords <= 0) return null;

        CharMgr.GetNearbyCharacters(Owner.Position, VisionRadius, NearbyCharacters);

        CharacterBase best = null;
        float bestDistSq = float.MaxValue;
        Vector3 myPos = Owner.Position;

        int count = NearbyCharacters.Count;
        for (int i = 0; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == Owner || other.CurrentHp <= 0f) continue;

            SwordOrbit otherOrbit = other.GetSwordOrbit();
            int otherSwords = otherOrbit != null ? otherOrbit.SwordCount : 0;

            if (otherSwords >= mySwords) continue;

            float dx = other.Position.x - myPos.x;
            float dy = other.Position.y - myPos.y;
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
    /// Tìm kiếm rơi có path distance ngắn nhất trong tầm nhìn.
    /// </summary>
    public Sword FindBestSword()
    {
        if (ItemMgr == null || Pathfinder == null) return null;

        ItemMgr.GetNearbySwords(Owner.Position, VisionRadius, NearbySwords);

        Sword best = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = Owner.Position;

        int count = NearbySwords.Count;
        for (int i = 0; i < count; i++)
        {
            Sword s = NearbySwords[i];
            float dist = Pathfinder.FindPathDistance(myPos, s.Position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = s;
            }
        }

        return best;
    }

    /// <summary>
    /// Tốc độ di chuyển hiện tại (có tính flee boost).
    /// </summary>
    public float GetCurrentSpeed()
    {
        float speed = Owner.MoveSpeed;
        if (FleeBoostTimer > 0f)
            speed *= FleeSpeedMultiplier;
        return speed;
    }

    /// <summary>
    /// Kích hoạt flee speed boost nếu chưa cooldown.
    /// </summary>
    public bool TryActivateFleeBoost()
    {
        if (FleeCooldownTimer > 0f) return false;

        FleeBoostTimer = FleeSpeedDuration;
        FleeCooldownTimer = FleeSpeedCooldown;
        return true;
    }
}
