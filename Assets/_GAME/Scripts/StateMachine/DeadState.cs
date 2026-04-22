using UnityEngine;

public class DeadState : ICharacterState
{
    private float deathTimer;
    private const float DEATH_DELAY = 1f;

    public void Enter(CharacterStateMachine sm)
    {
        deathTimer = 0f;
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        deathTimer += deltaTime;

        if (deathTimer >= DEATH_DELAY)
        {
            sm.Owner.OnDespawn();
        }
    }

    public void Exit(CharacterStateMachine sm) { }
}
