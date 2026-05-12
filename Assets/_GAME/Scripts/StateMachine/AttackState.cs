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

    private const float RepathInterval = 0.5f;
    private const float FleeChaseTimeout = 2f;
    private const float OptimalAttackDistance = 2f;
    private const float AttackDistanceTolerance = 0.5f;
    private const float ChaseSpeedBonus = 0.5f;
    private const float OrbitAngleStep = 45f;
    private const float OrbitArriveThreshold = 0.5f;
    private const float OrbitArriveThresholdSq = OrbitArriveThreshold * OrbitArriveThreshold;
    private const float AttackSpeedMultiplier = 1.2f;
    private static readonly float[] ChaseAngleOffsets = { 0.785f, -0.785f, 1.57f, -1.57f };

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
            currentOrbitAngle = Mathf.Atan2(toChar.z, toChar.x) * Mathf.Rad2Deg;
            orbitClockwise = Random.value > 0.5f;
            
            BuildPathToTarget(sm);
        }
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if (sm.Owner.IsKnockedBack) return;

        int mySwords = sm.MySwordCount;
        bool isLocked = sm.Owner.IsTargetLocked;
        
        if (mySwords <= 0 && !isLocked)
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
        float dz = targetPos.z - sm.CachedPosition.z;
        float distSq = dx * dx + dz * dz;

        if (!isLocked && distSq > sm.VisionRadiusSq * 1.2f)
        {
            sm.ChangeState(sm.Wander);
            return;
        }

        float currentDist = Mathf.Sqrt(distSq);
        float distanceError = currentDist - OptimalAttackDistance;

        if (distanceError < -AttackDistanceTolerance)
        {
            Vector3 retreatDir = new Vector3(-dx, 0f, -dz).normalized;
            Vector3 retreatTarget = sm.CachedPosition + retreatDir * Mathf.Abs(distanceError);
            
            if (sm.Map != null)
            {
                retreatTarget = sm.Map.ClampToMap(retreatTarget);
                if (!sm.Map.IsBlockedWorld(retreatTarget))
                    sm.MoveToward(retreatTarget, sm.GetCurrentSpeed() * AttackSpeedMultiplier + ChaseSpeedBonus, deltaTime);
            }
            else
            {
                sm.MoveToward(retreatTarget, sm.GetCurrentSpeed() * AttackSpeedMultiplier + ChaseSpeedBonus, deltaTime);
            }
            return;
        }

        if (distanceError > AttackDistanceTolerance)
        {
            if ((repathTimer -= deltaTime) <= 0f)
            {
                repathTimer = RepathInterval;
                Vector3 chaseTarget = CalculateChaseOrbitPosition(sm, targetPos);
                BuildPathToPosition(sm, chaseTarget);
            }

            sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed() * AttackSpeedMultiplier + ChaseSpeedBonus, deltaTime);
            return;
        }

        UpdateOrbitMovement(sm, targetPos, deltaTime);
    }

    public void Exit(CharacterStateMachine sm)
    {
        target = null;
    }

    private void UpdateOrbitMovement(CharacterStateMachine sm, Vector3 targetPos, float deltaTime)
    {
        if (currentOrbitPosition == Vector3.zero)
        {
            PickNextOrbitPosition(sm, targetPos);
            return;
        }

        Vector3 myPos = sm.CachedPosition;
        float dox = myPos.x - currentOrbitPosition.x, doz = myPos.z - currentOrbitPosition.z;
        float dtx = myPos.x - targetPos.x, dtz = myPos.z - targetPos.z;
        float distToTarget = Mathf.Sqrt(dtx * dtx + dtz * dtz);

        if (Mathf.Abs(distToTarget - OptimalAttackDistance) > AttackDistanceTolerance)
        {
            PickNextOrbitPosition(sm, targetPos);
            return;
        }

        if (dox * dox + doz * doz <= OrbitArriveThresholdSq)
        {
            PickNextOrbitPosition(sm, targetPos);
            return;
        }

        sm.MoveToward(currentOrbitPosition, sm.GetCurrentSpeed() * AttackSpeedMultiplier, deltaTime, OrbitArriveThreshold);
    }

    private void PickNextOrbitPosition(CharacterStateMachine sm, Vector3 targetPos)
    {
        Vector3 toChar = sm.CachedPosition - targetPos;
        float currentAngle = Mathf.Atan2(toChar.z, toChar.x) * Mathf.Rad2Deg;

        float newAngle = currentAngle;
        if (orbitClockwise) newAngle -= OrbitAngleStep;
        else                newAngle += OrbitAngleStep;

        while (newAngle <   0f) newAngle += 360f;
        while (newAngle >= 360f) newAngle -= 360f;

        currentOrbitAngle = newAngle;

        float angleRad    = newAngle * Mathf.Deg2Rad;
        float orbitRadius = OptimalAttackDistance;

        Vector3 orbitPos = new Vector3(
            targetPos.x + Mathf.Cos(angleRad) * orbitRadius,
            targetPos.y,
            targetPos.z + Mathf.Sin(angleRad) * orbitRadius
        );

        if (sm.Map != null)
        {
            orbitPos = sm.Map.ClampToMap(orbitPos);

            if (sm.Map.IsBlockedWorld(orbitPos))
            {
                orbitClockwise = !orbitClockwise;

                newAngle = orbitClockwise
                    ? currentAngle - OrbitAngleStep
                    : currentAngle + OrbitAngleStep;

                while (newAngle <   0f) newAngle += 360f;
                while (newAngle >= 360f) newAngle -= 360f;

                angleRad = newAngle * Mathf.Deg2Rad;
                orbitPos = new Vector3(
                    targetPos.x + Mathf.Cos(angleRad) * orbitRadius,
                    targetPos.y,
                    targetPos.z + Mathf.Sin(angleRad) * orbitRadius
                );
                orbitPos = sm.Map.ClampToMap(orbitPos);

                if (sm.Map.IsBlockedWorld(orbitPos)) return;
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

    private void BuildPathToPosition(CharacterStateMachine sm, Vector3 targetPosition)
    {
        pathIndex = 0;
        if (sm.Pathfinder != null)
            sm.Pathfinder.FindPath(sm.CachedPosition, targetPosition, sm.PathBuffer);
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(targetPosition);
        }
    }

    private Vector3 CalculateChaseOrbitPosition(CharacterStateMachine sm, Vector3 targetPos)
    {
        Vector3 toChar = sm.CachedPosition - targetPos;
        float currentAngle = Mathf.Atan2(toChar.z, toChar.x);

        Vector3 orbitPos = new Vector3(
            targetPos.x + Mathf.Cos(currentAngle) * OptimalAttackDistance,
            targetPos.y,
            targetPos.z + Mathf.Sin(currentAngle) * OptimalAttackDistance
        );

        if (sm.Map != null)
        {
            orbitPos = sm.Map.ClampToMap(orbitPos);

            if (sm.Map.IsBlockedWorld(orbitPos))
            {
                for (int i = 0; i < ChaseAngleOffsets.Length; i++)
                {
                    float testAngle = currentAngle + ChaseAngleOffsets[i];
                    Vector3 testPos = new Vector3(
                        targetPos.x + Mathf.Cos(testAngle) * OptimalAttackDistance,
                        targetPos.y,
                        targetPos.z + Mathf.Sin(testAngle) * OptimalAttackDistance
                    );
                    testPos = sm.Map.ClampToMap(testPos);
                    if (!sm.Map.IsBlockedWorld(testPos))
                        return testPos;
                }
                return targetPos;
            }
        }

        return orbitPos;
    }
}
