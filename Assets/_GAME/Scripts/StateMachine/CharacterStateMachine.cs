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
    [SerializeField] private float targetSwitchCooldown = 0.5f;

    private float visionRadiusSq;
    private float lastTargetSwitchTime;
    private CharacterBase lastAttacker;

    public CharacterBase Owner => owner;
    public SwordOrbit Orbit => orbit;
    public MapManager Map => map;
    public GridPathfinder Pathfinder => map?.Pathfinder;
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
        
        map = MapManager.Instance;
        charMgr = CharacterManager.Instance;
        itemMgr = ItemManager.Instance;
        visionRadiusSq = visionRadius * visionRadius;
    }

    public void OnInit()
    {
        PathBuffer.Clear();
        NearbyCharacters.Clear();
        NearbySwords.Clear();
        StateTimer = 0f;
        lastTargetSwitchTime = 0f;
        lastAttacker = null;
        CachedPosition = owner.transform.position;
        ChangeState(Wander);
    }

    public void OnDespawn()
    {
        PathBuffer.Clear();
        NearbyCharacters.Clear();
        NearbySwords.Clear();
        CurrentState?.Exit(this);
        CurrentState = null;
        StateTimer = 0f;
        lastTargetSwitchTime = 0f;
        lastAttacker = null;
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
        if (CurrentState == Dead || owner.CurrentHp <= 0f || attacker == null)
            return;

        float currentTime = Time.time;
        bool canSwitch = currentTime - lastTargetSwitchTime >= targetSwitchCooldown;

        if (CurrentState == Attack)
        {
            CharacterBase currentTarget = Attack.GetTarget();
            
            if (currentTarget == null)
            {
                Attack.SetTarget(attacker);
                lastAttacker = attacker;
                lastTargetSwitchTime = currentTime;
            }
            else if (canSwitch && ShouldSwitchTarget(currentTarget, attacker))
            {
                Attack.SetTarget(attacker);
                lastAttacker = attacker;
                lastTargetSwitchTime = currentTime;
            }
            return;
        }

        if (CurrentState == Flee)
        {
            CharacterBase currentThreat = Flee.GetThreat();
            
            if (currentThreat == null)
            {
                Flee.SetThreat(attacker);
                lastAttacker = attacker;
                lastTargetSwitchTime = currentTime;
            }
            else if (canSwitch && ShouldSwitchTarget(currentThreat, attacker))
            {
                Flee.SetThreat(attacker);
                lastAttacker = attacker;
                lastTargetSwitchTime = currentTime;
            }
            return;
        }

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
        
        lastAttacker = attacker;
        lastTargetSwitchTime = currentTime;
    }

    private bool ShouldSwitchTarget(CharacterBase current, CharacterBase newAttacker)
    {
        int currentSwords = current.SwordCount;
        int newSwords = newAttacker.SwordCount;
        
        if (newSwords != currentSwords) return newSwords > currentSwords;

        float distToCurrent = (current.TF.position - CachedPosition).sqrMagnitude;
        float distToNew = (newAttacker.TF.position - CachedPosition).sqrMagnitude;
        return distToNew < distToCurrent;
    }

    public int MySwordCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => orbit?.SwordCount ?? 0;
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
            nextX = target.x;
            nextY = target.y;
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

        dx = target.x - nextX;
        dy = target.y - nextY;
        return dx * dx + dy * dy <= threshSq;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ValidateMove(float fromX, float fromY, ref float toX, ref float toY, float z)
    {
        if (map == null) return;

        Vector3 to = new Vector3(toX, toY, z);
        Vector3 mid = new Vector3((fromX + toX) * 0.5f, (fromY + toY) * 0.5f, z);
        
        if (!map.IsBlockedWorld(to) && !map.IsBlockedWorld(mid)) return;

        Vector3 tryX = new Vector3(toX, fromY, z);
        Vector3 midX = new Vector3((fromX + toX) * 0.5f, fromY, z);
        if (!map.IsBlockedWorld(tryX) && !map.IsBlockedWorld(midX))
        {
            toY = fromY;
            return;
        }

        Vector3 tryY = new Vector3(fromX, toY, z);
        Vector3 midY = new Vector3(fromX, (fromY + toY) * 0.5f, z);
        if (!map.IsBlockedWorld(tryY) && !map.IsBlockedWorld(midY))
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
        charMgr.GetNearbyCharacters(Owner.transform.position, visionRadius * 0.65f, NearbyCharacters);

        CharacterBase best = null;
        float bestDistSq = float.MaxValue;
        float myX = CachedPosition.x, myY = CachedPosition.y;

        for (int i = 0, count = NearbyCharacters.Count; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == owner || other.CurrentHp <= 0f || other.SwordCount > mySwords) continue;

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
