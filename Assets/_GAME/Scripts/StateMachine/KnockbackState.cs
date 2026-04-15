using UnityEngine;

/// <summary>
/// Bị đẩy lùi, mất quyền điều khiển.
/// Khi hết knockbackTimer → quay về state phù hợp.
/// </summary>
public class KnockbackState : ICharacterState
{
    public void Enter(CharacterStateMachine sm) { }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        CharacterBase owner = sm.Owner;

        // Knockback physics được xử lý trong CharacterBase (knockbackVelocity + decay)
        // State này chỉ chờ hết knockback rồi chuyển state

        if (!owner.IsKnockedBack)
        {
            // Hết knockback → quyết định state tiếp theo
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
                    // Có kiếm → phản đòn
                    sm.Attack.SetTarget(attacker);
                    sm.ChangeState(sm.Attack);
                }
                else
                {
                    // Không kiếm → chạy trốn
                    sm.Flee.SetThreat(attacker);
                    sm.ChangeState(sm.Flee);
                }
            }
            else
            {
                // Không có threat → tìm kiếm hoặc wander
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
