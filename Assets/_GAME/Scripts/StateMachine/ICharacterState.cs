public interface ICharacterState
{
    void Enter(CharacterStateMachine sm);
    void Execute(CharacterStateMachine sm, float deltaTime);
    void Exit(CharacterStateMachine sm);
}
