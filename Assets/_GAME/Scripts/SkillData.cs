using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Main Effect — Projectile / Attack chính")]
    public GameObject mainEffect;

    [Header("Hand Effect — Hiệu ứng chuẩn bị trên tay")]
    public GameObject handEffect;

    [Header("Character Effect — Aura / Buff trên người")]
    public GameObject characterEffect;

    [Tooltip("Thời gian tồn tại của tất cả effect (giây)")]
    public float effectLifeTime = 8f;
    [Tooltip("Scale của toàn bộ effect (1 = mặc định)")]
    public float effectScale = 1f;

    [Header("Combat")]
    public float damage = 20f;
    public float cooldown = 1.5f;
    [Tooltip("Thời gian đứng yên khi thực hiện skill (giây, 0 = không dừng)")]
    public float castDuration = 0f;

    [Header("Camera Shake")]
    public float shakeOnFireDuration  = 0f;
    public float shakeOnFireMagnitude = 0.3f;
    [Tooltip("Delay trước khi rung camera (giây)")]
    public float shakeDelay = 0f;
    [Tooltip("Số mục tiêu tối đa bị ảnh hưởng (1 = single target)")]
    public int maxTargets = 1;
    [Tooltip("Bán kính vùng sát thương tính từ điểm va chạm (0 = chỉ trúng target chính)")]
    public float damageRadius = 0f;

    [Header("Hit Effect")]
    [Tooltip("Hiệu ứng áp lên mục tiêu khi trúng skill")]
    public SkillHitEffect hitEffect = SkillHitEffect.None;
    [Tooltip("Thời gian stun (giây)")]
    public float stunDuration = 0.5f;
    [Tooltip("Lực knockback của skill (0 = dùng lực mặc định)")]
    public float skillKnockbackForce = 10f;
    [Tooltip("Thời gian bay của knockback (giây)")]
    public float skillKnockbackDuration = 0.2f;

    [Header("Slow Effect")]
    [Tooltip("Bật hiệu ứng làm chậm khi trúng skill")]
    public bool enableSlow = false;
    [Tooltip("Hệ số chậm (0 = đứng yên hoàn toàn, 1 = không chậm)")]
    [Range(0f, 1f)]
    public float slowFactor = 0.5f;
    [Tooltip("Thời gian làm chậm (giây)")]
    public float slowDuration = 2f;
}

public enum SkillHitEffect
{
    None,
    Stun,
    KnockbackStun
}
