using UnityEngine;

public class FleeState : ICharacterState
{
    private CharacterBase threat;
    private int pathIndex;
    private float fleeTimer;
    private float repathTimer;

    private const float FleeDuration = 2f;
    private const float RepathInterval = 0.3f;
    private const float FleeDistance = 10f;

    public void SetThreat(CharacterBase t) => threat = t;
    public CharacterBase GetThreat() => threat;

    public void Enter(CharacterStateMachine sm)
    {
        fleeTimer = 0f;
        repathTimer = 0f;
        sm.Owner.ActivateFleeProtection();
        CalculateFleeTarget(sm);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        if ((fleeTimer += deltaTime) >= FleeDuration)
        {
            sm.ChangeState(sm.Wander);
            return;
        }

        if ((repathTimer -= deltaTime) <= 0f)
        {
            repathTimer = RepathInterval;
            CalculateFleeTarget(sm);
        }

        if (sm.MoveAlongPath(ref pathIndex, sm.GetCurrentSpeed(), deltaTime))
            CalculateFleeTarget(sm);
    }

    public void Exit(CharacterStateMachine sm) => threat = null;

    private void CalculateFleeTarget(CharacterStateMachine sm)
    {
        Vector3 myPos = sm.CachedPosition;
        
        if (threat == null || threat.CurrentHp <= 0f || !threat.gameObject.activeInHierarchy)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 randomTarget = new Vector3(
                myPos.x + Mathf.Cos(angle) * FleeDistance,
                myPos.y + Mathf.Sin(angle) * FleeDistance,
                myPos.z);

            if (sm.Map != null)
                randomTarget = sm.Map.ClampToMap(randomTarget);

            BuildPath(sm, myPos, randomTarget);
            return;
        }

        Vector3 threatPos = threat.TF.position;
        float dx = myPos.x - threatPos.x;
        float dy = myPos.y - threatPos.y;
        float mag = Mathf.Sqrt(dx * dx + dy * dy);

        if (mag < 0.01f)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            dx = Mathf.Cos(angle);
            dy = Mathf.Sin(angle);
        }
        else
        {
            dx /= mag;
            dy /= mag;
        }

        Vector3 fleeTarget = new Vector3(
            myPos.x + dx * FleeDistance,
            myPos.y + dy * FleeDistance,
            myPos.z);

        if (sm.Map != null)
        {
            fleeTarget = sm.Map.ClampToMap(fleeTarget);
            
            if (sm.Map.IsWall(fleeTarget))
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 0.785398f;
                    float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    float rdx = dx * cos - dy * sin;
                    float rdy = dx * sin + dy * cos;
                    
                    Vector3 alt = new Vector3(
                        myPos.x + rdx * FleeDistance,
                        myPos.y + rdy * FleeDistance,
                        myPos.z);
                    
                    alt = sm.Map.ClampToMap(alt);
                    
                    if (!sm.Map.IsWall(alt))
                    {
                        fleeTarget = alt;
                        break;
                    }
                }
            }
        }

        BuildPath(sm, myPos, fleeTarget);
    }

    private void BuildPath(CharacterStateMachine sm, Vector3 from, Vector3 to)
    {
        pathIndex = 0;
        
        if (sm.Pathfinder != null)
        {
            if (sm.Pathfinder.FindPath(from, to, sm.PathBuffer) >= float.MaxValue || sm.PathBuffer.Count == 0)
            {
                sm.PathBuffer.Clear();
                sm.PathBuffer.Add(to);
            }
        }
        else
        {
            sm.PathBuffer.Clear();
            sm.PathBuffer.Add(to);
        }
    }
}
