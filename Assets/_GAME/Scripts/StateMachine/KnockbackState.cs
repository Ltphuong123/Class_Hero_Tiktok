using UnityEngine;

public class KnockbackState : ICharacterState
{
    public void Enter(CharacterStateMachine sm) { }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        CharacterBase owner = sm.Owner;

        if (!owner.IsKnockedBack)
        {
            if (owner.CurrentHp <= 0f)
            {
                sm.ChangeState(sm.Dead);
                return;
            }

            int mySwords = sm.MySwordCount;
            CharacterBase attacker = owner.LastAttacker;

            if (attacker != null && attacker.CurrentHp > 0f && attacker.gameObject.activeInHierarchy)
            {
                if (mySwords > 0)
                {
                    sm.Attack.SetTarget(attacker);
                    sm.ChangeState(sm.Attack);
                }
                else
                {
                    sm.Flee.SetThreat(attacker);
                    sm.ChangeState(sm.Flee);
                }
            }
            else
            {
                Sword sword = sm.FindBestSword();
                if (sword != null)
                {
                    sm.CollectSword.SetTargetSword(sword);
                    sm.ChangeState(sm.CollectSword);
                }
                else
                {
                    sm.ChangeState(sm.Wander);
                }
            }
        }
    }

    public void Exit(CharacterStateMachine sm) { }
}
