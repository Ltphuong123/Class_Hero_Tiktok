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
    [SerializeField] private float stateMinDuration = 0.4f;

    private float visionRadiusSq;

    public CharacterBase Owner => owner;
    public SwordOrbit Orbit => orbit;
    public MapManager Map => map;
    public GridPathfinder Pathfinder => map != null ? map.Pathfinder : null;
    public CharacterManager CharMgr => charMgr;
    public ItemManager ItemMgr => itemMgr;

    public float VisionRadius => visionRadius;
    public float VisionRadiusSq => visionRadiusSq;
    public float StateMinDuration => stateMinDuration;

    public ICharacterState CurrentState { get; private set; }
    public float StateTimer { get; set; }

    public Vector3 CachedPosition { get; private set; }

    public readonly List<Vector3> PathBuffer = new(32);
    public readonly List<CharacterBase> NearbyCharacters = new(16);
    public readonly List<Sword> NearbySwords = new(16);

    public readonly WanderState Wander = new();
    public readonly CollectSwordState CollectSword = new();
    public readonly AttackState Attack = new();
    public readonly FleeState Flee = new();
    public readonly DeadState Dead = new();

    private void Awake()
    {
        if (owner == null) owner = GetComponent<CharacterBase>();
        if (orbit == null && owner != null) orbit = owner.GetSwordOrbit();
        
        if (map == null) map = MapManager.Instance;
        if (charMgr == null) charMgr = CharacterManager.Instance;
        if (itemMgr == null) itemMgr = ItemManager.Instance;

        // Cache squared values
        visionRadiusSq = visionRadius * visionRadius;

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

    public void OnUnderAttack(CharacterBase attacker)
    {
        if (CurrentState == Dead) return;
        if (owner.CurrentHp <= 0f) { ChangeState(Dead); return; }

        if (attacker == null) return;

        // Nếu đang Attack hoặc Flee, chỉ đổi target nếu attacker mới nguy hiểm hơn
        if (CurrentState == Attack)
        {
            CharacterBase currentTarget = Attack.GetTarget();
            if (currentTarget != null && ShouldSwitchTarget(currentTarget, attacker))
            {
                Attack.SetTarget(attacker);
            }
            else if (currentTarget == null)
            {
                Attack.SetTarget(attacker);
            }
            return;
        }

        if (CurrentState == Flee)
        {
            CharacterBase currentThreat = Flee.GetThreat();
            if (currentThreat != null && ShouldSwitchTarget(currentThreat, attacker))
            {
                Flee.SetThreat(attacker);
            }
            else if (currentThreat == null)
            {
                Flee.SetThreat(attacker);
            }
            return;
        }

        // Chưa có target, quyết định Attack hoặc Flee
        if (MySwordCount > 3)
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

    private bool ShouldSwitchTarget(CharacterBase current, CharacterBase newAttacker)
    {
        // Ưu tiên kẻ địch có nhiều kiếm hơn (nguy hiểm hơn)
        int currentSwords = current.SwordCount;
        int newSwords = newAttacker.SwordCount;
        
        if (newSwords > currentSwords) return true;
        if (newSwords < currentSwords) return false;

        // Nếu số kiếm bằng nhau, chọn kẻ gần hơn
        float distToCurrent = (current.TF.position - CachedPosition).sqrMagnitude;
        float distToNew = (newAttacker.TF.position - CachedPosition).sqrMagnitude;
        
        return distToNew < distToCurrent;
    }

    public int MySwordCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => orbit != null ? orbit.SwordCount : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetCurrentSpeed() => owner.MoveSpeed;

    public bool MoveToward(Vector3 target, float speed, float deltaTime, float arriveThreshold = 0.3f)
    {
        float posX = CachedPosition.x, posY = CachedPosition.y, posZ = CachedPosition.z;
        float dx = target.x - posX, dy = target.y - posY;
        float distSq = dx * dx + dy * dy;
        float threshSq = arriveThreshold * arriveThreshold;

        if (distSq <= threshSq) return true;

        float step = speed * deltaTime;
        float nextX, nextY;

        if (step * step >= distSq)
        {
            nextX = target.x; nextY = target.y;
        }
        else
        {
            float invDist = step / Mathf.Sqrt(distSq);
            nextX = posX + dx * invDist;
            nextY = posY + dy * invDist;
        }

        ValidateMove(posX, posY, ref nextX, ref nextY, posZ);

        float movedSq = (nextX - posX) * (nextX - posX) + (nextY - posY) * (nextY - posY);
        if (movedSq < 0.001f) return true;

        Vector3 newPos = new Vector3(nextX, nextY, posZ);
        owner.transform.position = newPos;
        CachedPosition = newPos;

        dx = target.x - nextX; dy = target.y - nextY;
        return dx * dx + dy * dy <= threshSq;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ValidateMove(float fromX, float fromY, ref float toX, ref float toY, float z)
    {
        if (map == null) return;

        if (!map.IsBlockedWorld(new Vector3(toX, toY, z)) && 
            !map.IsBlockedWorld(new Vector3((fromX + toX) * 0.5f, (fromY + toY) * 0.5f, z)))
            return;

        if (!map.IsBlockedWorld(new Vector3(toX, fromY, z)) && 
            !map.IsBlockedWorld(new Vector3((fromX + toX) * 0.5f, fromY, z)))
        {
            toY = fromY;
            return;
        }

        if (!map.IsBlockedWorld(new Vector3(fromX, toY, z)) && 
            !map.IsBlockedWorld(new Vector3(fromX, (fromY + toY) * 0.5f, z)))
        {
            toX = fromX;
            return;
        }

        toX = fromX;
        toY = fromY;
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
        charMgr.GetNearbyCharacters(Owner.transform.position, visionRadius*0.65f, NearbyCharacters);

        CharacterBase best = null;
        float bestDistSq = float.MaxValue;
        float myX = CachedPosition.x, myY = CachedPosition.y;

        for (int i = 0, count = NearbyCharacters.Count; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == owner || other.CurrentHp <= 0f) continue;
            if (other.IsFleeProtected) continue;

            int otherSwords = other.SwordCount;
            if (otherSwords > mySwords) continue;

            Vector3 pos = other.TF.position;
            float dx = pos.x - myX, dy = pos.y - myY;
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

        Sword best = null;
        float bestDistSq = float.MaxValue;
        float myX = CachedPosition.x, myY = CachedPosition.y;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = NearbySwords[i].TF.position;
            float dx = pos.x - myX, dy = pos.y - myY;
            float distSq = dx * dx + dy * dy;
            
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = NearbySwords[i];
            }
        }

        return best;
    }




}
