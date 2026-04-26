using UnityEngine;

public class AttackState : ICharacterState
{
    private CharacterBase target;
    private int pathIndex;
    private float repathTimer;
    private float chaseTimer;
    private Vector3 currentOrbitPosition;
    private float currentOrbitAngle;
    private bool orbitClockwise;
    private float orbitHoldTimer;

    private const float RepathInterval = 0.5f;
    private const float FleeChaseTimeout = 2f;
    private const float OptimalAttackDistance = 3f;
    private const float OptimalAttackDistanceSq = OptimalAttackDistance * OptimalAttackDistance;
    private const float AttackDistanceTolerance = 0.5f;
    private const float ChaseSpeedBonus = 0.5f;
    private const float OrbitAngleStep = 45f;
    private const float OrbitArriveThreshold = 0.5f;

    public void SetTarget(CharacterBase t) => target = t;
    public CharacterBase GetTarget() => target;

    public void Enter(CharacterStateMachine sm)
    {
        repathTimer = 0f;
        chaseTimer = 0f;
        pathIndex = 0;
        currentOrbitPosition = Vector3.zero;
        
        if (target != null)
        {
            Vector3 toChar = sm.CachedPosition - target.TF.position;
            currentOrbitAngle = Mathf.Atan2(toChar.y, toChar.x) * Mathf.Rad2Deg;
            orbitClockwise = Random.value > 0.5f;
            
            BuildPathToTarget(sm);
        }
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if (sm.Owner.IsKnockedBack) return;

        int mySwords = sm.MySwordCount;
        bool isLocked = sm.Owner.IsTargetLocked;
        
        if (!isLocked && mySwords <= 0)
        {
            if (target != null) sm.Flee.SetThreat(target);
            sm.ChangeState(sm.Flee);
            return;
        }

        if (target == null || target.CurrentHp <= 0f || !target.gameObject.activeInHierarchy)
        {
            if (isLocked)
            {
                sm.Owner.UnlockTarget();
            }
            sm.ChangeState(sm.Wander);
            return;
        }

        if (!isLocked && mySwords <= 3 && target.SwordCount >= mySwords)
        {
            sm.Flee.SetThreat(target);
            sm.ChangeState(sm.Flee);
            return;
        }

        if (target.GetStateMachine()?.CurrentState is FleeState)
        {
            if ((chaseTimer += deltaTime) >= FleeChaseTimeout)
            {
                if (!isLocked)
                {
                    sm.ChangeState(sm.Wander);
                    return;
                }
            }
        }
        else
        {
            chaseTimer = 0f;
        }

        Vector3 targetPos = target.TF.position;
        float dx = targetPos.x - sm.CachedPosition.x;
        float dy = targetPos.y - sm.CachedPosition.y;
        float distSq = dx * dx + dy * dy;

        if (!isLocked && distSq > sm.VisionRadiusSq * 1.2f)
        {
            sm.ChangeState(sm.Wander);
            return;
        }

        float currentDist = Mathf.Sqrt(distSq);
        float distanceError = currentDist - OptimalAttackDistance;

        if (distanceError < -AttackDistanceTolerance)
        {
            Vector3 retreatDir = new Vector3(-dx, -dy, 0f).normalized;
            Vector3 retreatTarget = sm.CachedPosition + retreatDir * Mathf.Abs(distanceError);
            
            if (sm.Map != null)
            {
                retreatTarget = sm.Map.ClampToMap(retreatTarget);
                if (!sm.Map.IsBlockedWorld(retreatTarget))
                    sm.MoveToward(retreatTarget, sm.GetCurrentSpeed() + ChaseSpeedBonus, deltaTime);
            }
            else
            {
                sm.MoveToward(retreatTarget, sm.GetCurrentSpeed() + ChaseSpeedBonus, deltaTime);
            }
            return;
        }

        if (distanceError > AttackDistanceTolerance)
        {
            if ((repathTimer -= deltaTime) <= 0f)
            {
                repathTimer = RepathInterval;
                BuildPathToTarget(sm);
            }

            sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed() + ChaseSpeedBonus, deltaTime);
            return;
        }

        UpdateOrbitMovement(sm, targetPos, deltaTime);
    }

    public void Exit(CharacterStateMachine sm) => target = null;

    private void UpdateOrbitMovement(CharacterStateMachine sm, Vector3 targetPos, float deltaTime)
    {
        if (currentOrbitPosition == Vector3.zero)
        {
            PickNextOrbitPosition(sm, targetPos);
            return;
        }
        
        float distToOrbit = Vector3.Distance(sm.CachedPosition, currentOrbitPosition);
        float distToTarget = Vector3.Distance(sm.CachedPosition, targetPos);
        float distanceError = distToTarget - OptimalAttackDistance;
        
        if (Mathf.Abs(distanceError) > AttackDistanceTolerance)
        {
            PickNextOrbitPosition(sm, targetPos);
            return;
        }
        
        if (distToOrbit <= OrbitArriveThreshold)
        {
            PickNextOrbitPosition(sm, targetPos);
            return;
        }
        
        sm.MoveToward(currentOrbitPosition, sm.GetCurrentSpeed(), deltaTime, OrbitArriveThreshold);
    }

    private void PickNextOrbitPosition(CharacterStateMachine sm, Vector3 targetPos)
    {
        Vector3 toChar = sm.CachedPosition - targetPos;
        float currentAngle = Mathf.Atan2(toChar.y, toChar.x) * Mathf.Rad2Deg;
        
        float newAngle = currentAngle;
        if (orbitClockwise)
            newAngle -= OrbitAngleStep;
        else
            newAngle += OrbitAngleStep;
        
        while (newAngle < 0f) newAngle += 360f;
        while (newAngle >= 360f) newAngle -= 360f;
        
        currentOrbitAngle = newAngle;
        
        float angleRad = newAngle * Mathf.Deg2Rad;
        float orbitRadius = OptimalAttackDistance;
        
        Vector3 orbitPos = new Vector3(
            targetPos.x + Mathf.Cos(angleRad) * orbitRadius,
            targetPos.y + Mathf.Sin(angleRad) * orbitRadius,
            targetPos.z
        );
        
        if (sm.Map != null)
        {
            orbitPos = sm.Map.ClampToMap(orbitPos);
            
            if (sm.Map.IsBlockedWorld(orbitPos))
            {
                orbitClockwise = !orbitClockwise;
                
                if (orbitClockwise)
                    newAngle = currentAngle - OrbitAngleStep;
                else
                    newAngle = currentAngle + OrbitAngleStep;
                
                while (newAngle < 0f) newAngle += 360f;
                while (newAngle >= 360f) newAngle -= 360f;
                
                angleRad = newAngle * Mathf.Deg2Rad;
                orbitPos = new Vector3(
                    targetPos.x + Mathf.Cos(angleRad) * orbitRadius,
                    targetPos.y + Mathf.Sin(angleRad) * orbitRadius,
                    targetPos.z
                );
                orbitPos = sm.Map.ClampToMap(orbitPos);
                
                if (sm.Map.IsBlockedWorld(orbitPos))
                {
                    return;
                }
            }
        }
        
        currentOrbitPosition = orbitPos;
    }

    private void BuildPathToTarget(CharacterStateMachine sm)
    {
        if (target == null) return;
        pathIndex = 0;
        if (sm.Pathfinder != null)
            sm.Pathfinder.FindPath(sm.CachedPosition, target.TF.position, sm.PathBuffer);
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(target.TF.position);
        }
    }
}
