using UnityEngine;

/// <summary>
/// Đuổi theo và tấn công CharacterBase yếu hơn.
/// Giữ khoảng cách AttackKeepDistance để kiếm orbit chạm target.
/// Nếu hết kiếm → Flee. Nếu target chết/mất → quay về scan.
/// </summary>
public class AttackState : ICharacterState
{
    private CharacterBase target;
    private float repathTimer;

    private const float RepathInterval = 0.3f;

    public void SetTarget(CharacterBase t) => target = t;

    public void Enter(CharacterStateMachine sm)
    {
        repathTimer = 0f;
    }

    public void Execute(CharacterStateMachine sm, float deltaTime)
    {
        // Hết kiếm → Flee
        if (sm.MySwordCount <= 0)
        {
            if (target != null)
                sm.Flee.SetThreat(target);
            sm.ChangeState(sm.Flee);
            return;
        }

        // Target không hợp lệ → tìm target mới hoặc quay về collect/wander
        if (target == null || target.CurrentHp <= 0f || !target.gameObject.activeInHierarchy)
        {
            target = null;
            CharacterBase newTarget = sm.FindWeakerTarget();
            if (newTarget != null)
            {
                target = newTarget;
            }
            else
            {
                // Không còn ai để đánh → tìm kiếm hoặc wander
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
                return;
            }
        }

        // Kiểm tra target còn trong tầm nhìn
        Vector3 myPos = sm.Owner.Position;
        Vector3 targetPos = target.Position;
        float dx = targetPos.x - myPos.x;
        float dy = targetPos.y - myPos.y;
        float distSq = dx * dx + dy * dy;
        float visionSq = sm.VisionRadius * sm.VisionRadius;

        if (distSq > visionSq * 1.2f) // thêm 20% hysteresis tránh ping-pong
        {
            target = null;
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
            return;
        }

        // Di chuyển đuổi theo target, giữ khoảng cách AttackKeepDistance
        float dist = Mathf.Sqrt(distSq);
        float keepDist = sm.AttackKeepDistance;

        if (dist > keepDist + 0.2f)
        {
            // Đuổi theo - di chuyển thẳng về phía target
            float speed = sm.GetCurrentSpeed();
            float step = speed * deltaTime;
            float inv = step / dist;
            if (inv > 1f) inv = 1f;

            // Dừng lại ở khoảng cách keepDist
            float moveRatio = Mathf.Min(inv, (dist - keepDist) / dist);
            sm.Owner.transform.position = new Vector3(
                myPos.x + dx * moveRatio,
                myPos.y + dy * moveRatio,
                myPos.z
            );
        }
        else if (dist < keepDist - 0.3f)
        {
            // Quá gần → lùi ra một chút
            float speed = sm.GetCurrentSpeed() * 0.5f;
            float step = speed * deltaTime;
            float inv = step / dist;
            if (inv > 1f) inv = 1f;

            sm.Owner.transform.position = new Vector3(
                myPos.x - dx * inv,
                myPos.y - dy * inv,
                myPos.z
            );
        }
        // Nếu đúng khoảng cách → đứng yên, kiếm orbit tự tấn công
    }

    public void Exit(CharacterStateMachine sm)
    {
        target = null;
    }
}
