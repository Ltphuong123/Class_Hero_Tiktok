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

    [Header("Attack Effects")]
    [SerializeField] private GameObject attackEffectObject;
    [SerializeField] private ParticleSystem attackParticle;

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
        SetAttackEffects(false);
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
        if (owner.IsCasting) return;
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

        if (owner.IsTargetLocked)
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
            if (MySwordCount > 3)
            {
                Attack.SetTarget(attacker);
                ChangeState(Attack);
                lastAttacker = attacker;
                lastTargetSwitchTime = currentTime;
                return;
            }

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

        bool shouldFlee = MySwordCount <= 0 || (MySwordCount <= 3 && attacker.SwordCount >= MySwordCount);
        if (shouldFlee)
        {
            Flee.SetThreat(attacker);
            ChangeState(Flee);
        }
        else
        {
            Attack.SetTarget(attacker);
            ChangeState(Attack);
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
        get => owner?.SwordCount ?? 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetCurrentSpeed() => owner.MoveSpeed;

    public bool MoveToward(Vector3 target, float speed, float deltaTime, float arriveThreshold = 0.3f)
    {
        float posX = CachedPosition.x, posY = CachedPosition.y, posZ = CachedPosition.z;
        float dx = target.x - posX, dz = target.z - posZ;
        float distSq = dx * dx + dz * dz;
        float threshSq = arriveThreshold * arriveThreshold;

        if (distSq <= threshSq) return true;

        float step = speed * deltaTime;
        float nextX, nextZ;

        if (step * step >= distSq)
        {
            nextX = target.x;
            nextZ = target.z;
        }
        else
        {
            float invDist = step / Mathf.Sqrt(distSq);
            nextX = posX + dx * invDist;
            nextZ = posZ + dz * invDist;
        }

        ValidateMove(posX, posZ, ref nextX, ref nextZ, posY);

        float movedSq = (nextX - posX) * (nextX - posX) + (nextZ - posZ) * (nextZ - posZ);
        if (movedSq < 0.001f) return true;

        Vector3 newPos = new Vector3(nextX, posY, nextZ);
        owner.transform.position = newPos;
        CachedPosition = newPos;

        dx = target.x - nextX;
        dz = target.z - nextZ;
        return dx * dx + dz * dz <= threshSq;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ValidateMove(float fromX, float fromZ, ref float toX, ref float toZ, float y)
    {
        if (map == null) return;

        Vector3 to  = new Vector3(toX,   y, toZ);
        Vector3 mid = new Vector3((fromX + toX) * 0.5f, y, (fromZ + toZ) * 0.5f);

        if (!map.IsBlockedWorld(to) && !map.IsBlockedWorld(mid)) return;

        Vector3 tryX  = new Vector3(toX,   y, fromZ);
        Vector3 midX  = new Vector3((fromX + toX) * 0.5f, y, fromZ);
        if (!map.IsBlockedWorld(tryX) && !map.IsBlockedWorld(midX))
        {
            toZ = fromZ;
            return;
        }

        Vector3 tryZ  = new Vector3(fromX, y, toZ);
        Vector3 midZ  = new Vector3(fromX, y, (fromZ + toZ) * 0.5f);
        if (!map.IsBlockedWorld(tryZ) && !map.IsBlockedWorld(midZ))
        {
            toX = fromX;
            return;
        }

        toX = fromX;
        toZ = fromZ;
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

    public void SetAttackEffects(bool active)
    {
        if (attackEffectObject != null) attackEffectObject.SetActive(active);
        if (attackParticle != null)
        {
            if (active) attackParticle.Play();
            else        attackParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public CharacterBase FindWeakerTarget()
    {
        if (charMgr == null) return null;
        
        int mySwords = MySwordCount;
        charMgr.GetNearbyCharacters(Owner.transform.position, visionRadius, NearbyCharacters);

        CharacterBase best = null;
        float bestDistSq = float.MaxValue;
        float myX = CachedPosition.x, myZ = CachedPosition.z;

        for (int i = 0, count = NearbyCharacters.Count; i < count; i++)
        {
            CharacterBase other = NearbyCharacters[i];
            if (other == owner || other.CurrentHp <= 0f || other.SwordCount > mySwords) continue;

            Vector3 pos = other.TF.position;
            float dx = pos.x - myX, dz = pos.z - myZ;
            float distSq = dx * dx + dz * dz;
            
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
        float myX = CachedPosition.x, myZ = CachedPosition.z;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = NearbySwords[i].TF.position;
            float dx = pos.x - myX, dz = pos.z - myZ;
            float distSq = dx * dx + dz * dz;
            
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = NearbySwords[i];
            }
        }

        return best;
    }
}
