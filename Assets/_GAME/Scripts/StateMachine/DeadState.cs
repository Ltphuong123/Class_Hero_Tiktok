public class DeadState : ICharacterState
{
    public void Enter(CharacterStateMachine sm)
    {
        SwordOrbit orbit = sm.Orbit;
        if (orbit != null)
        {
            int count = orbit.SwordCount;
            for (int i = count - 1; i >= 0; i--)
                orbit.DropSword(i);
        }

        sm.Owner.gameObject.SetActive(false);
    }

    public void Execute(CharacterStateMachine sm, float deltaTime) { }

    public void Exit(CharacterStateMachine sm) { }
}
