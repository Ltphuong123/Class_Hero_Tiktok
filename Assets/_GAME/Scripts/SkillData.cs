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
}
