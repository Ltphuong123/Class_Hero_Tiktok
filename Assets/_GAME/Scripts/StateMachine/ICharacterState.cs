/// <summary>
/// Interface cho mỗi state trong CharacterBase state machine.
/// </summary>
public interface ICharacterState
{
    void Enter(CharacterStateMachine sm);
    void Execute(CharacterStateMachine sm, float deltaTime);
    void Exit(CharacterStateMachine sm);
}
